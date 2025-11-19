namespace Survivor.Runtime.Data
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// Contains a reference to the unique avatar prefab
    /// </summary>
    public struct AvatarPrefabContainer : IComponentData
    {
        public Entity PlayerAvatarPrefab;
    }

    /// <summary>
    /// Contains a reference to tall the enemy prefabs
    /// </summary>
    public struct EnemyPrefabsContainer : IBufferElementData
    {
        public Entity EnemyPrefab;
    }

    /// <summary>
    /// Authoring component designed to reference all the prefabs used in the game.
    /// </summary>
    public class EntityPrefabsContainerAuthoring : MonoBehaviour
    {
        public GameObject PlayerAvatarPrefab;
        public GameObject[] EnemiesPrefabs;

        class Baker : Baker<EntityPrefabsContainerAuthoring>
        {
            public override void Bake(EntityPrefabsContainerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var container = new AvatarPrefabContainer
                {
                    PlayerAvatarPrefab = GetEntity(authoring.PlayerAvatarPrefab, TransformUsageFlags.Dynamic),
                };
                AddComponent(entity, container);
                
                var buffer = AddBuffer<EnemyPrefabsContainer>(entity);
                for (int i = 0; i < authoring.EnemiesPrefabs.Length; i++)
                {
                    buffer.Add(new EnemyPrefabsContainer()
                        { EnemyPrefab = GetEntity(authoring.EnemiesPrefabs[i], TransformUsageFlags.Dynamic) });
                }
            }
        }
    }
}