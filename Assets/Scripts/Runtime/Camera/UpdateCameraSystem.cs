namespace Survivor.Runtime.Camera
{
    using UnityEngine;
    using Unity.Mathematics;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Updates the transform camera entity according to the target entity.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    partial struct UpdateCameraSystem : ISystem
    {
        private EntityQuery _cameraTargetQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _cameraTargetQuery = SystemAPI.QueryBuilder().WithAll<CameraTarget, LocalToWorld>().Build();
            state.RequireForUpdate(_cameraTargetQuery);
            state.RequireForUpdate<CameraEntity>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_cameraTargetQuery.CalculateEntityCount() > 1)
            {
                Debug.LogError("There should be only one camera target entity.");
                return;
            }

            new UpdateCameraTransformJob()
            {
                CameraEntity = SystemAPI.GetSingletonEntity<CameraEntity>(),
                // We also need to update the LocalTransform and not just the LocalToWorld,
                // or the LocalTransform will never be computed from the LocalToWorld is used to compute the camera position.
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(false)
            }.Schedule(_cameraTargetQuery);
        }

        [BurstCompile]
        private partial struct UpdateCameraTransformJob : IJobEntity
        {
            public Entity CameraEntity;
            
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            private void Execute(Entity cameraTargetEntity, in CameraTarget cameraTarget)
            {
                // Can't access directly in the execute signature (because of the lookups), or I should access it with a "ref". Would it be better?
                var localToWorld = LocalToWorldLookup[cameraTargetEntity];
                var newPosition = localToWorld.Position + cameraTarget.OffsetToTarget;
                LocalToWorldLookup[CameraEntity] = new LocalToWorld()
                {
                    Value = float4x4.TRS(newPosition, cameraTarget.Rotation,
                        new float3(1f))
                };
                
                LocalTransformLookup[CameraEntity] = new LocalTransform()
                {
                    Position = newPosition, 
                    Rotation = cameraTarget.Rotation,
                    Scale = 1f
                };
            }
        }
    }
}