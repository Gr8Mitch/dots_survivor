namespace Survivor.Runtime.Lifecycle
{
    using Unity.Burst;
    using Unity.Entities;
    using Survivor.Runtime.Character;

    // TODO: also add PendingDestruction to all the entities of the LinkedEntityGroup so that we can destroy the entities
    // directly through the EntityQuery? Maybe it would be much faster.
    /// <summary>
    /// A tag component to mark a entity as dead, waiting to be destroyed.
    /// </summary>
    public struct PendingDestruction : IComponentData { }
    
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    partial struct DeathSystem : ISystem
    {
        private EntityQuery _nonAvatarPendingDestructionQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _nonAvatarPendingDestructionQuery = SystemAPI
                .QueryBuilder()
                .WithAll<PendingDestruction>()
                .WithNone<AvatarCharacterComponent>().Build();
            
            state.RequireForUpdate(_nonAvatarPendingDestructionQuery);
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
    }
}