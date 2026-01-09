namespace Survivor.Runtime.Enemy
{
    using Unity.Mathematics;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Survivor.Runtime.Character;

    /// <summary>
    /// Used to spawn enemies continuously.
    /// </summary>
    public struct EnemySpawner : IComponentData
    {
        // TODO_IMPROVEMENT: add a way to spawn multiple enemies at once + multiple types of enemies.
        /// <summary>
        /// The enemy type to spawn.
        /// </summary>
        public EnemyTypeId EnemyTypeId;
        
        /// <summary>
        /// The max radius (in meters) around the spawner where enemies can spawn.
        /// </summary>
        public float SpawnRadius;
        
        /// <summary>
        /// The min/max interval between each spawn (in seconds).
        /// </summary>
        public float2 SpawnInterval;

        /// <summary>
        /// The time left (in seconds) until the next spawn.
        /// </summary>
        public float TimeToNextSpawn;
        
        /// <summary>
        /// The elapsed time when happened the last spawn.
        /// </summary>
        public double LastSpawnTime;
        
        /// <summary>
        /// The random generator used for this spawner.
        /// </summary>
        public Unity.Mathematics.Random Random;
    }
    
    class EnemySpawnerAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The enemy type to spawn.
        /// </summary>
        [FormerlySerializedAs("EnemyType")] 
        [EnemyTypeIdReference]
        public EnemyTypeId EnemyTypeId;
        
        /// <summary>
        /// The radius (in meters) around the spawner where enemies can spawn.
        /// </summary>
        public float SpawnRadius;
        
        /// <summary>
        /// The min/max interval between each spawn (in seconds).
        /// </summary>
        public Vector2 SpawnInterval;
        
        class EnemySpawnerAuthoringBaker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawner()
                {
                    EnemyTypeId = authoring.EnemyTypeId,
                    SpawnRadius = authoring.SpawnRadius,
                    SpawnInterval = authoring.SpawnInterval,
                    LastSpawnTime = float.MinValue
                });
            }
        }
    }
}