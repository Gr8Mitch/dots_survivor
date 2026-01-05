namespace Survivor.Runtime.Lifecycle
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Transforms;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Enemy;
    using Unity.Burst.Intrinsics;

    /// <summary>
    /// A singleton that contains all the damages dealt to entities.
    /// </summary>
    public struct DamagesContainer : IComponentData
    {
        public struct DamageData
        {
            public float3 Position;
            public ushort Damages;
        }
        
        public NativeParallelMultiHashMap<Entity, DamageData> DamagesPerEntity;

        public DamagesContainer(int capacity, Allocator allocator)
        {
            DamagesPerEntity = new NativeParallelMultiHashMap<Entity, DamageData>(capacity, allocator);
        }

        public void Dispose()
        {
            DamagesPerEntity.Dispose();
        }
    }

    /// <summary>
    /// Contains all the data related to the entities that could potentially receive damages.
    /// </summary>
    public readonly struct DamageReceiverData
    {
        public readonly float3 Position;
        public readonly float HitBoxRadius;
        public readonly Entity Entity;

        public DamageReceiverData(float3 position, float hitBoxRadius, Entity entity)
        {
            Position = position;
            HitBoxRadius = hitBoxRadius;
            Entity = entity;
        }
    }
    
    /// <summary>
    /// A singleton component that contains all the damage receivers entities + some additional data.
    /// Gets refreshed every frame.
    /// </summary>
    public struct DamageReceiversContainer : IComponentData
    {
        public NativeList<DamageReceiverData> EnemyDamageReceivers;
        public NativeReference<DamageReceiverData> PlayerDamageReceiver;

        public void Dispose()
        {
            EnemyDamageReceivers.Dispose();
            PlayerDamageReceiver.Dispose();
        }

        public void EnsureCapacity(int damageReceiversCount)
        {
            if (EnemyDamageReceivers.Capacity < damageReceiversCount)
            {
                // Resize the container now, we can't do it in the job because of the ParallelWriter.
                EnemyDamageReceivers.Resize(math.ceilpow2(damageReceiversCount), NativeArrayOptions.UninitializedMemory);
            }
        }
    }
    
    /// <summary>
    /// Fetches the entities that can receive damages.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct DamageReceiversFetcherSystem : ISystem
    {
        private EntityQuery _damageReceiversQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _damageReceiversQuery = SystemAPI.QueryBuilder().WithAll<HealthComponent, LocalTransform>().Build();
            
            // Create the DamageReceiversContainer singleton
            DamageReceiversContainer damageReceiversContainer = new DamageReceiversContainer()
            {
                EnemyDamageReceivers =
                    new NativeList<DamageReceiverData>(math.ceilpow2(EnemySpawnSystem.MAX_ENEMIES),
                        Allocator.Persistent),
                PlayerDamageReceiver = new NativeReference<DamageReceiverData>(Allocator.Persistent)
            };
            state.EntityManager.CreateSingleton(damageReceiversContainer);
            
            state.RequireForUpdate<DamageReceiversContainer>();
            state.RequireForUpdate(_damageReceiversQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Destroys the DamageReceiversContainer singleton
            if (SystemAPI.TryGetSingleton<DamageReceiversContainer>(out var container))
            {
                container.Dispose();
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DamageReceiversContainer>());
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRW<DamageReceiversContainer>();
            var damageReceiversContainer = SystemAPI.GetSingletonRW<DamageReceiversContainer>();
            damageReceiversContainer.ValueRW.EnsureCapacity(_damageReceiversQuery.CalculateEntityCount());

            damageReceiversContainer.ValueRW.EnemyDamageReceivers.Clear();
            
            new FetchEnemyDamageReceiverDataJob()
            {
                DamageReceivers = damageReceiversContainer.ValueRW.EnemyDamageReceivers.AsParallelWriter()
            }.ScheduleParallel();

            new FetchPlayerDamageReceiverDataJob()
            {
                PlayerData = damageReceiversContainer.ValueRW.PlayerDamageReceiver
            }.Schedule();
        }
        
        [BurstCompile]
        [WithAll(typeof(EnemyCharacterComponent))]
        private partial struct FetchEnemyDamageReceiverDataJob : IJobEntity
        {
            public NativeList<DamageReceiverData>.ParallelWriter DamageReceivers;
            
            private void Execute(Entity entity, in HealthComponent healthComponent, in LocalTransform localTransform)
            {
                DamageReceivers.AddNoResize(new DamageReceiverData(localTransform.Position, healthComponent.HitBoxRadius, entity));
            }
        }
        
        [BurstCompile]
        [WithAll(typeof(AvatarCharacterComponent))]
        private partial struct FetchPlayerDamageReceiverDataJob : IJobEntity
        {
            public NativeReference<DamageReceiverData> PlayerData;
            
            private void Execute(Entity entity, in HealthComponent healthComponent, in LocalTransform localTransform)
            {
                PlayerData.Value = new DamageReceiverData(localTransform.Position, healthComponent.HitBoxRadius, entity);
            }
        }
    }
    
    // TODO_IMPROVEMENT: if the system is too close to DamageReceiversFetcherSystem, there could be a (little) sync point here.
    // Do not hesitate to add more systems in between.
    /// <summary>
    /// Computes all the damages that needs to be dealt and stores them in the DamagesContainer singleton.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(DamageReceiversFetcherSystem))]
    [BurstCompile]
    public partial struct DamagesSystem : ISystem
    {
        private const int DAMAGES_CONTAINER_INITIAL_CAPACITY = 128;
        
        private EntityQuery _damageReceiversQuery;
        private EntityQuery _damageDealerQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _damageDealerQuery = SystemAPI.QueryBuilder().WithAll<DamageDealer, LocalToWorld>().Build();
            _damageReceiversQuery = SystemAPI.QueryBuilder().WithAll<HealthComponent, LocalTransform>().Build();
            
            state.EntityManager.CreateSingleton<DamagesContainer>(
                new DamagesContainer(DAMAGES_CONTAINER_INITIAL_CAPACITY, Allocator.Persistent));
            
            state.RequireForUpdate<DamagesContainer>();
            state.RequireForUpdate<AvatarCharacterComponent>();
            state.RequireForUpdate<DamageReceiversContainer>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate(_damageReceiversQuery);
            state.RequireForUpdate(_damageDealerQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<DamagesContainer>(out var container))
            {
                container.Dispose();
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DamagesContainer>());
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRW<DamagesContainer>();
            var damagesContainer = SystemAPI.GetSingletonRW<DamagesContainer>();
            ref var damagesPerEntity = ref damagesContainer.ValueRW.DamagesPerEntity;
            damagesPerEntity.Clear();
            if (damagesPerEntity.Capacity < _damageReceiversQuery.CalculateEntityCount() + 1)
            {
                damagesPerEntity.Capacity = math.ceilpow2(_damageDealerQuery.CalculateEntityCount() + 1);
            }
            
            state.EntityManager.CompleteDependencyBeforeRO<DamageReceiversContainer>();
            var damagesReceiversContainer = SystemAPI.GetSingleton<DamageReceiversContainer>();
            var ecbParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var job = new ComputeDamagesJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                DamageDealerTypeHandle = SystemAPI.GetComponentTypeHandle<DamageDealer>(true),
                LocalToWorldTypeHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                DamageSphereZoneTypeHandle = SystemAPI.GetComponentTypeHandle<DamageSphereZone>(true),
                TargetedDamagesTypeHandle = SystemAPI.GetComponentTypeHandle<TargetedDamages>(true),
                DestroyOnDamageTypeHandle = SystemAPI.GetComponentTypeHandle<DestroyOnDamage>(true),
                DamageCooldownTypeHandle = SystemAPI.GetComponentTypeHandle<DamageCooldown>(false),
                EnemyDamageReceivers = damagesReceiversContainer.EnemyDamageReceivers,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                DamagesPerEntity = damagesPerEntity.AsParallelWriter(),
                PlayerData = damagesReceiversContainer.PlayerDamageReceiver,
                EcbParallel = ecbParallel
            };
            state.Dependency = job.ScheduleParallel(_damageDealerQuery, state.Dependency);
        }
        
        [BurstCompile]
        private struct ComputeDamagesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityTypeHandle;
            
            [ReadOnly]
            public ComponentTypeHandle<DamageDealer> DamageDealerTypeHandle;
            
            /// <summary>
            /// We can't rely on the LocalTransform here as the damage dealer entity can be a child entity.
            /// </summary>
            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandle;
            
            [ReadOnly]
            public ComponentTypeHandle<DamageSphereZone> DamageSphereZoneTypeHandle;
            
            [ReadOnly]
            public ComponentTypeHandle<TargetedDamages> TargetedDamagesTypeHandle;
            
            [ReadOnly]
            public ComponentTypeHandle<DestroyOnDamage> DestroyOnDamageTypeHandle;
            
            public ComponentTypeHandle<DamageCooldown> DamageCooldownTypeHandle;
            
            [ReadOnly]
            public NativeList<DamageReceiverData> EnemyDamageReceivers;
            
            public NativeParallelMultiHashMap<Entity, DamagesContainer.DamageData>.ParallelWriter DamagesPerEntity;
            
            [ReadOnly]
            public double ElapsedTime;

            [ReadOnly]
            public NativeReference<DamageReceiverData> PlayerData;
            
            public EntityCommandBuffer.ParallelWriter EcbParallel;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // TODO : check if we really have performance difference using the pointers.
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                var damageDealers = chunk.GetNativeArray(ref DamageDealerTypeHandle);
                var localToWorlds = chunk.GetNativeArray(ref LocalToWorldTypeHandle);
                bool isDamageSphereZone = chunk.Has<DamageSphereZone>();
                var damageSphereZones = isDamageSphereZone ? chunk.GetNativeArray(ref DamageSphereZoneTypeHandle) : default;
                bool isTargetedDamages = chunk.Has<TargetedDamages>();
                var targetedDamages = isTargetedDamages ? chunk.GetNativeArray(ref TargetedDamagesTypeHandle) : default;
                bool isCooldownDamage = chunk.Has<DamageCooldown>();
                var damageCooldowns = isCooldownDamage ? chunk.GetNativeArray(ref DamageCooldownTypeHandle) : default;
                bool areDamagesForEnemies = chunk.Has<DamagesToEnemy>();

                bool mustBeDestroyedOnDamage = chunk.Has<DestroyOnDamage>();
                
                // Cooldown: first check if the cooldown is over. Not need to check further if not
                // Sphere zone: do the damages to all eligible the receivers in the zone
                // Targeted damages: do the damages only to the targeted receiver if in the receiver hit box zone.
                if (areDamagesForEnemies)
                {
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        if (isCooldownDamage && !IsCooldownOver(ref damageCooldowns, i, ElapsedTime))
                        {
                            continue;
                        }

                        float zoneRadius = isDamageSphereZone ? damageSphereZones[i].Radius : 0f;
                        float3 damagePosition = localToWorlds[i].Position;
                        ushort damages = damageDealers[i].Damages;

                        bool hasInflictedDamages = false;
                        // Check all the enemies to see if the damages can be dealt to them.
                        foreach (var enemyDamageReceiver in EnemyDamageReceivers)
                        {
                            if (isTargetedDamages && targetedDamages[i].Target != enemyDamageReceiver.Entity)
                            {
                                // TODO: do a separate loop specific for isTargetedDamages for performance purpose? 
                                continue;
                            }

                            if (CanHit(in damagePosition, in enemyDamageReceiver.Position,
                                    enemyDamageReceiver.HitBoxRadius, zoneRadius))
                            {
                                DamagesPerEntity.Add(enemyDamageReceiver.Entity, new DamagesContainer.DamageData()
                                {
                                    Damages = damages,
                                    Position = enemyDamageReceiver.Position
                                });
                                hasInflictedDamages = true;
                            }
                        }

                        if (hasInflictedDamages && mustBeDestroyedOnDamage)
                        {
                            // TODO: create a common flow for the projectiles to be destroyed, like adding a command somewhere, or create an entity.
                            EcbParallel.DestroyEntity(unfilteredChunkIndex, entities[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        if (isCooldownDamage && !IsCooldownOver(ref damageCooldowns, i, ElapsedTime))
                        {
                            continue;
                        }

                        float zoneRadius = isDamageSphereZone ? damageSphereZones[i].Radius : 0f;
                        float3 damagePosition = localToWorlds[i].Position;
                        
                        var playerData = PlayerData.Value;
                        if (CanHit(in damagePosition, in playerData.Position,
                                playerData.HitBoxRadius, zoneRadius))
                        {
                            ushort damages = damageDealers[i].Damages;
                            DamagesPerEntity.Add(playerData.Entity, new DamagesContainer.DamageData()
                            {
                                Damages = damages,
                                Position = damagePosition
                            });
                            
                            if (mustBeDestroyedOnDamage)
                            {
                                // The entity will be destroyed in the next frame.
                                EcbParallel.AddComponent<PendingDestruction>(unfilteredChunkIndex, entities[i]);
                            }
                        }
                    }
                }
            }
            
            private bool IsCooldownOver(ref NativeArray<DamageCooldown> damageCooldowns, int entityIndex, double elapsedTime)
            {
                var coolDown = damageCooldowns[entityIndex];
                if (!coolDown.IsCooldownOver(ElapsedTime))
                {
                    return false;
                }
                else
                {
                    damageCooldowns[entityIndex] = coolDown.OnDamagesDone(ElapsedTime);
                    return true;
                }
            }
            
            private bool CanHit(in float3 damagePosition, in float3 damageReceiverPosition, float hitBoxRadius, float zoneRadius)
            {
                float sqrDistanceToHit = (hitBoxRadius + zoneRadius) *
                                         (hitBoxRadius + zoneRadius);
                            
                float sqrDistance = math.distancesq(damagePosition, damageReceiverPosition);
                return sqrDistance <= sqrDistanceToHit;
            }
        }
    }
}