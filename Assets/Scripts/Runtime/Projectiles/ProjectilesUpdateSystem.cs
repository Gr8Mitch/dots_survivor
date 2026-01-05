namespace Survivor.Runtime.Projectiles
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Physics;
    using Unity.Transforms;
    using Survivor.Runtime.Common;
    using Survivor.Runtime.Physics;
    using Unity.Mathematics;
    using Survivor.Runtime.Lifecycle;

    /// <summary>
    /// Updates the position of all the projectiles according to their velocity and destroy them if they hit something.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    partial struct ProjectilesUpdateSystem : ISystem
    {
        private CollisionFilter _castToEnvironmentCollisionFilter;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Projectile>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            _castToEnvironmentCollisionFilter = PhysicsUtilities.BuildFilterForRaycast(LayerConstants.ENVIRONMENT_LAYER);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Not worth parallelizing this job.
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            new UpdateProjectilesJob()
            {
                PhysicsWorld = physicsWorld,
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.DeltaTime,
                CastToEnvironmentCollisionFilter = _castToEnvironmentCollisionFilter
            }.Schedule();
        }

        [BurstCompile]
        private partial struct UpdateProjectilesJob : IJobEntity
        {
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;
            
            public EntityCommandBuffer Ecb;
            
            public float DeltaTime;
            
            public CollisionFilter CastToEnvironmentCollisionFilter;
            
            private void Execute(Entity entity, ref LocalTransform localTransform, in Projectile projectile)
            {
                float3 newPosition = localTransform.Position + projectile.Velocity * DeltaTime;
                
                // Do a raycast to see if they hit something from the environment
                RaycastInput raycastInput = new RaycastInput()
                {
                    Start = localTransform.Position,
                    End = newPosition,
                    Filter = CastToEnvironmentCollisionFilter
                };
                bool hasCollided = PhysicsWorld.CastRay(raycastInput);
                if (hasCollided)
                {
                    // The projectile will be destroyed in the next frame.
                    Ecb.AddComponent<PendingDestruction>(entity);
                }
                else
                {
                    localTransform.Position = newPosition;
                }
            }
        }
    }
}