namespace Survivor.Runtime.Lifecycle
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// A tag component to know where to spawn the player avatar.
    /// </summary>
    public struct PlayerAvatarSpawner : IComponentData {}
    
    class PlayerAvatarSpawnerAuthoring : MonoBehaviour
    {
        class PlayerAvatarSpawnerAuthoringBaker : Baker<PlayerAvatarSpawnerAuthoring>
        {
            public override void Bake(PlayerAvatarSpawnerAuthoring authoring)
            {
                // Should probably have been TransformUsageFlags.WorldSpace but it is simpler this way because we
                // only need to access the LocalTransform on both entities in <see cref="AvatarSpawnerSystem"/>.
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerAvatarSpawner());
            }
        }
    }
}