namespace Survivor.Runtime.Lifecycle
{
    using Unity.Entities;
    using Survivor.Runtime.Data;
    using Unity.Burst;
    using Unity.Transforms;
    using Unity.Mathematics;
    
    [BurstCompile]
    public partial struct PlayerAvatarSpawnSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AvatarPrefabContainer>();
            // TODO: for now, we only have one. Maybe it would be interesting to have multiple spawners and choose one
            // randomly ?
            state.RequireForUpdate<PlayerAvatarSpawner>();
        }
        
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            SpawnPlayerAvatar(ref state);
            
            // TODO: Add a mechanism to have a component existing only when the avatar is not spawned, so that
            // we can use a related query to not update the system when the avatar is indeed spawned.
        }

        public void OnStopRunning(ref SystemState state) { }

        private void SpawnPlayerAvatar(ref SystemState state)
        {
            var avatarPrefabContainer = SystemAPI.GetSingleton<AvatarPrefabContainer>();
            var avatarSpawnerEntity = SystemAPI.GetSingletonEntity<PlayerAvatarSpawner>();
            var avatarEntity = state.WorldUnmanaged.EntityManager.Instantiate(avatarPrefabContainer.PlayerAvatarPrefab);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);

            var avatarSpawnTransform = localTransformLookup[avatarSpawnerEntity];
            // TODO: get the real half height
            // Adding an offset
            avatarSpawnTransform.Position += new float3(0f, 2.0f, 0f);
            localTransformLookup[avatarEntity] = avatarSpawnTransform;
        }
    }
}