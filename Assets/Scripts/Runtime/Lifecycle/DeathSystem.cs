namespace Survivor.Runtime.Lifecycle
{
    using Unity.Burst;
    using Unity.Entities;
    using Survivor.Runtime.Character;
    
    /// <summary>
    /// Destroys entities with a <see cref="PendingDestruction"/> component.
    /// Also handles the entities with a <see cref="LimitedLifetime"/> component.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    partial struct DeathSystem : ISystem
    {
        private EntityQuery _nonAvatarPendingDestructionQuery;
        private EntityQuery _limitedLifetimeQuery;
        
        public void OnCreate(ref SystemState state)
        {
            _nonAvatarPendingDestructionQuery = SystemAPI
                .QueryBuilder()
                .WithAll<PendingDestruction>()
                .WithNone<AvatarCharacterComponent>().Build();
            
            _limitedLifetimeQuery = SystemAPI
                .QueryBuilder()
                .WithAll<LimitedLifetime>()
                .WithNone<PendingDestruction>()
                .Build();
            
            state.RequireAnyForUpdate(_nonAvatarPendingDestructionQuery, _limitedLifetimeQuery);
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // We could have used ecb.DestroyEntity(_nonAvatarPendingDestructionQuery) but it is not possible because of the
            // limitations introduced with the LinkedEntityGroup.
            new DestroyNonAvatarEntitiesJob()
            {
                Ecb = ecb
            }.Schedule(_nonAvatarPendingDestructionQuery);

            new UpdateLimitedLifetimeEntities()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb
            }.Schedule(_limitedLifetimeQuery);
        }

        /// <summary>
        /// Destroys all the entities marked as dead, expect for the player avatar.
        /// </summary>
        [BurstCompile]
        private partial struct DestroyNonAvatarEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity)
            {
                Ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        private partial struct UpdateLimitedLifetimeEntities : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer Ecb;
            
            public void Execute(Entity entity, ref LimitedLifetime lifetime)
            {
                lifetime.RemainingLifetime -= DeltaTime;
                if (lifetime.RemainingLifetime <= 0f)
                {
                    Ecb.AddComponent<PendingDestruction>(entity);
                }
            }
        }
    }
}