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
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(false)
            }.Schedule(_cameraTargetQuery);
        }

        [BurstCompile]
        private partial struct UpdateCameraTransformJob : IJobEntity
        {
            public Entity CameraEntity;
            
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            private void Execute(Entity cameraTargetEntity, in CameraTarget cameraTarget)
            {
                // Can't access directly in the execute signature, or I should have access it with a "ref". Would it be better?
                var localToWorld = LocalToWorldLookup[cameraTargetEntity];
                LocalToWorldLookup[CameraEntity] = new LocalToWorld()
                {
                    Value = float4x4.TRS(
                        localToWorld.Position + cameraTarget.OffsetToTarget,
                        cameraTarget.Rotation,
                        new float3(1f))
                };
            }
        }
    }
}