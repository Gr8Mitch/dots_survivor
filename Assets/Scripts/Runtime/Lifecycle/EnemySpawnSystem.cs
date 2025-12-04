namespace Survivor.Runtime.Lifecycle
{
    using Unity.Transforms;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Mathematics;
    using Survivor.Runtime.Data;
    using Survivor.Runtime.Character;
    using UnityEngine;

    /// <summary>
    /// Spawn enemies from the enemy spawners.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PlayerAvatarSpawnSystem))]
    [BurstCompile]
    partial struct EnemySpawnSystem : ISystem
    {
        private const int MAX_ENEMIES = 300;
        
        private EntityQuery _spawnerEntityQuery;
        private EntityQuery _enemiesQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _spawnerEntityQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld>().WithAllRW<EnemySpawner>().Build();
            _enemiesQuery = SystemAPI.QueryBuilder().WithAll<EnemyCharacterComponent>().Build();
            state.RequireForUpdate(_spawnerEntityQuery);
            state.RequireForUpdate<EnemyPrefabsContainer>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_enemiesQuery.CalculateEntityCount() >= MAX_ENEMIES)
            {
                return;
            }
            
            var enemiesPrefabs = SystemAPI.GetSingletonBuffer<EnemyPrefabsContainer>();
            var ecbParallel = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var enemyComponentLookup = SystemAPI.GetComponentLookup<EnemyCharacterComponent>(true);
            
            new SpawnEnemiesFromSpawnersJob()
            {
                EnemiesPrefabs = enemiesPrefabs,
                EnemyComponentLookup = enemyComponentLookup,
                CurrentTime = SystemAPI.Time.ElapsedTime,
                EcbParallel = ecbParallel
            }.ScheduleParallel(_spawnerEntityQuery);
        }

        [BurstCompile]
        private partial struct SpawnEnemiesFromSpawnersJob : IJobEntity
        {
            [ReadOnly]
            public DynamicBuffer<EnemyPrefabsContainer> EnemiesPrefabs;

            [ReadOnly]
            public ComponentLookup<EnemyCharacterComponent> EnemyComponentLookup;
            
            public double CurrentTime;
            
            public EntityCommandBuffer.ParallelWriter EcbParallel;
            
            public void Execute([ChunkIndexInQuery] int chunkIndex,
                Entity entity, 
                ref EnemySpawner enemySpawner,
                in LocalToWorld localToWorld)
            {
                if (enemySpawner.LastSpawnTime == float.MinValue)
                {
                    // The job is running for the first time for this spawner.
                    enemySpawner.Random = new Unity.Mathematics.Random((uint)(entity.Index * 1000));
                    UpdateSpawnData(ref enemySpawner);
                    // No need to spawn anything this time.
                    return;
                }
                
                if (CurrentTime - enemySpawner.LastSpawnTime >= enemySpawner.TimeToNextSpawn)
                {
                    // We need to spawn an enemy.
                    // First, find the right prefab to spawn.
                    Entity enemyPrefab = Entity.Null;
                    foreach (var entry in EnemiesPrefabs)
                    {
                        // TODO: make a hashmap or equivalent at initialzation time to find the prefab faster.
                        if (EnemyComponentLookup[entry.EnemyPrefab].EnemyTypeId == enemySpawner.EnemyTypeId)
                        {
                            enemyPrefab = entry.EnemyPrefab;
                            break;
                        }
                    }

                    if (enemyPrefab != Entity.Null)
                    {
                        // Spawn the prefab at the right position.
                        var enemyInstanceEntity = EcbParallel.Instantiate(chunkIndex, enemyPrefab);
                        float spawnRadius = enemySpawner.Random.NextFloat(0f, enemySpawner.SpawnRadius);
                        float2 spawnOffsetDirection = enemySpawner.Random.NextFloat2Direction();
                        float3 spawnPosition = localToWorld.Position 
                                               + new float3(spawnOffsetDirection.x, 0f,spawnOffsetDirection.y) * spawnRadius;
                        quaternion spawnRotation = enemySpawner.Random.NextQuaternionRotation();
                        // Extract forward, flatten it, rebuild rotation.
                        float3 forward = math.mul(spawnRotation, new float3(0,0,1));
                        forward.y = 0;
                        forward = math.normalizesafe(forward);

                        spawnRotation = quaternion.LookRotationSafe(forward, math.up());
                        
                        EcbParallel.SetComponent(chunkIndex, enemyInstanceEntity, new LocalTransform()
                        {
                            Position = spawnPosition,
                            Rotation = spawnRotation,
                            Scale = 1.0f
                        });
                    }
                    else
                    {
                        Debug.LogError($"Entity prefab of type {enemySpawner.EnemyTypeId} not found.");
                        return;
                    }
                    
                    UpdateSpawnData(ref enemySpawner);
                }
            }

            private void UpdateSpawnData(ref EnemySpawner enemySpawner)
            {
                enemySpawner.TimeToNextSpawn =
                    enemySpawner.Random.NextFloat(enemySpawner.SpawnInterval.x, enemySpawner.SpawnInterval.y);
                enemySpawner.LastSpawnTime = CurrentTime;
            }
        }
    }
}