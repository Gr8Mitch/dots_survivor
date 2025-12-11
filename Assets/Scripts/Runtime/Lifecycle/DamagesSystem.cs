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
    /// Contains all the damages dealt to entities.
    /// </summary>
    public struct DamagesContainer : IComponentData
    {
        public NativeParallelMultiHashMap<Entity, ushort> DamagesPerEntity;

        public DamagesContainer(int capacity, Allocator allocator)
        {
            DamagesPerEntity = new NativeParallelMultiHashMap<Entity, ushort>(capacity, allocator);
        }

        public void Dispose()
        {
            DamagesPerEntity.Dispose();
        }
    }
    
    /// <summary>
    /// Computes all the damages that needs to be dealt.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    partial struct DamagesSystem : ISystem
    {
        private const int DAMAGES_CONTAINER_INITIAL_CAPACITY = 128;
        
        private readonly struct DamageReceiverData
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
        
        private NativeList<DamageReceiverData> _enemyDamageReceivers;
        private EntityQuery _damageReceiversQuery;
        private EntityQuery _damageDealerQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _damageReceiversQuery = SystemAPI.QueryBuilder().WithAll<HealthComponent>().Build();
            _damageDealerQuery = SystemAPI.QueryBuilder().WithAll<DamageDealer, LocalToWorld>().Build();
            
            state.EntityManager.CreateSingleton<DamagesContainer>(
                new DamagesContainer(DAMAGES_CONTAINER_INITIAL_CAPACITY, Allocator.Persistent));
            _enemyDamageReceivers =
                new NativeList<DamageReceiverData>(math.ceilpow2(EnemySpawnSystem.MAX_ENEMIES), Allocator.Persistent);
            
            state.RequireForUpdate<DamagesContainer>();
            state.RequireForUpdate<AvatarCharacterComponent>();
            state.RequireForUpdate(_damageReceiversQuery);
            state.RequireForUpdate(_damageDealerQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _enemyDamageReceivers.Dispose();
            
            if (SystemAPI.TryGetSingleton<DamagesContainer>(out var container))
            {
                container.Dispose();
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DamagesContainer>());
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_enemyDamageReceivers.Capacity < _damageReceiversQuery.CalculateEntityCount())
            {
                // Resize the container now, we can't do it in the job because of the ParallelWriter.
                _enemyDamageReceivers.Resize(math.ceilpow2(_enemyDamageReceivers.Length), NativeArrayOptions.UninitializedMemory);
            }
            
            _enemyDamageReceivers.Clear();

            state.EntityManager.CompleteDependencyBeforeRW<DamagesContainer>();
            var damagesPerEntity = SystemAPI.GetSingletonRW<DamagesContainer>();
            damagesPerEntity.ValueRW.DamagesPerEntity.Clear();
            
            var playerSingletonEntity = SystemAPI.GetSingletonEntity<AvatarCharacterComponent>();
            var playerData = new DamageReceiverData(SystemAPI.GetComponent<LocalTransform>(playerSingletonEntity).Position,
                SystemAPI.GetComponent<HealthComponent>(playerSingletonEntity).HitBoxRadius, playerSingletonEntity);
                
            new FetchEnemyDamageReceiverDataJob()
            {
                DamageReceivers = _enemyDamageReceivers.AsParallelWriter()
            }.ScheduleParallel();
            
            var job = new ComputeDamagesJob()
            {
                DamageDealerTypeHandle = SystemAPI.GetComponentTypeHandle<DamageDealer>(true),
                LocalToWorldTypeHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                DamageSphereZoneTypeHandle = SystemAPI.GetComponentTypeHandle<DamageSphereZone>(true),
                TargetedDamagesTypeHandle = SystemAPI.GetComponentTypeHandle<TargetedDamages>(true),
                DamageCooldownTypeHandle = SystemAPI.GetComponentTypeHandle<DamageCooldown>(false),
                EnemyDamageReceivers = _enemyDamageReceivers,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                DamagesPerEntity = damagesPerEntity.ValueRW.DamagesPerEntity.AsParallelWriter(),
                PlayerData = playerData
            };
            state.Dependency = job.ScheduleParallel(_damageDealerQuery, state.Dependency);
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
        private struct ComputeDamagesJob : IJobChunk
        {
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
            
            public ComponentTypeHandle<DamageCooldown> DamageCooldownTypeHandle;
            
            [ReadOnly]
            public NativeList<DamageReceiverData> EnemyDamageReceivers;
            
            [ReadOnly]
            public DamageReceiverData PlayerData;
            
            public NativeParallelMultiHashMap<Entity, ushort>.ParallelWriter DamagesPerEntity;
            
            [ReadOnly]
            public double ElapsedTime;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // TODO : check if we really have performance difference using the pointers.
                var damageDealers = chunk.GetNativeArray(ref DamageDealerTypeHandle);
                var localToWorlds = chunk.GetNativeArray(ref LocalToWorldTypeHandle);
                bool isDamageSphereZone = chunk.Has<DamageSphereZone>();
                var damageSphereZones = isDamageSphereZone ? chunk.GetNativeArray(ref DamageSphereZoneTypeHandle) : default;
                bool isTargetedDamages = chunk.Has<TargetedDamages>();
                var targetedDamages = isTargetedDamages ? chunk.GetNativeArray(ref TargetedDamagesTypeHandle) : default;
                bool isCooldownDamage = chunk.Has<DamageCooldown>();
                var damageCooldowns = isCooldownDamage ? chunk.GetNativeArray(ref DamageCooldownTypeHandle) : default;
                bool areDamagesForEnemies = chunk.Has<DamagesToEnemy>();

                // TODO: handle destroy on hit.
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
                                DamagesPerEntity.Add(enemyDamageReceiver.Entity, damages);
                            }
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
                        
                        if (CanHit(in damagePosition, in PlayerData.Position,
                                PlayerData.HitBoxRadius, zoneRadius))
                        {
                            ushort damages = damageDealers[i].Damages;
                            DamagesPerEntity.Add(PlayerData.Entity, damages);
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