using Survivor.Runtime.Character;
using Survivor.Runtime.Controller;

namespace Survivor.Runtime.Player
{
    using Unity.Entities;
    using Survivor.Runtime.Data;
    using Unity.Burst;
    using Unity.Transforms;
    
    /// <summary>
    /// Spawns the player avatar at the spawner's location.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct PlayerAvatarSpawnSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AvatarPrefabContainer>();
            // TODO_IMPROVEMENT: for now, we only have one. Maybe it would be interesting to have multiple spawners and choose one
            // randomly ?
            state.RequireForUpdate<PlayerAvatarSpawner>();
            state.RequireForUpdate<CastCollidersContainer>();
        }
        
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            SpawnPlayerAvatar(ref state);
            
            // TODO_IMPROVEMENT: Add a mechanism to have a component existing only when the avatar is not spawned, so that
            // we can use a related query to not update the system when the avatar is indeed spawned (although this is clearly not prioritary)
        }

        public void OnStopRunning(ref SystemState state) { }

        private void SpawnPlayerAvatar(ref SystemState state)
        {
            var avatarPrefabContainer = SystemAPI.GetSingleton<AvatarPrefabContainer>();
            var avatarSpawnerEntity = SystemAPI.GetSingletonEntity<PlayerAvatarSpawner>();
            var castCollidersContainer = SystemAPI.GetSingleton<CastCollidersContainer>();
            var avatarEntity = state.WorldUnmanaged.EntityManager.Instantiate(avatarPrefabContainer.PlayerAvatarPrefab);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
            var avatarSpawnTransform = localTransformLookup[avatarSpawnerEntity];

            // No need to add an offset, the anchor of the characters are at the ground level.
            localTransformLookup[avatarEntity] = avatarSpawnTransform;
            
            // Set the cast colliders.
            var colliderData = castCollidersContainer.PlayerCastColliderData;
            SystemAPI.SetComponent(avatarEntity, new CharacterCastColliders()
            {
                CastColliderData = new CastColliderData()
                {
                    GroundCastCollider = colliderData.GroundCastCollider,
                    ObstacleCastCollider = colliderData.ObstacleCastCollider
                }
            });
        }
    }
}