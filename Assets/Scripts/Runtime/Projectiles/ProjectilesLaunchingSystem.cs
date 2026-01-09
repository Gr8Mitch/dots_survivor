namespace Survivor.Runtime.Projectiles
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Transforms;
    using Survivor.Runtime.Lifecycle;
    using Survivor.Runtime.Ability;
    using Survivor.Runtime.Character;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// The system is responsible on launching the projectiles.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(DamageReceiversFetcherSystem))]
    [BurstCompile]
    partial struct ProjectilesLaunchingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamageReceiversContainer>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRO<DamageReceiversContainer>();
            var damageReceiversContainer = SystemAPI.GetSingleton<DamageReceiversContainer>();
            // This is not really worth parallelizing it, it is not that heavy;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            new LaunchProjectilesJob()
            {
                DamageReceiversContainer = damageReceiversContainer,
                EnemyCharacterComponentLookup = SystemAPI.GetComponentLookup<EnemyCharacterComponent>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                Ecb = ecb,
                CurrentElapsedTime = SystemAPI.Time.ElapsedTime
            }.Schedule();
        }

        [BurstCompile]
        private partial struct LaunchProjectilesJob : IJobEntity
        {
            [ReadOnly]
            public DamageReceiversContainer DamageReceiversContainer;
            
            [ReadOnly]
            public ComponentLookup<EnemyCharacterComponent> EnemyCharacterComponentLookup;
            
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            
            public EntityCommandBuffer Ecb;
            
            public double CurrentElapsedTime;
            
            private void Execute(ref ProjectileLauncher projectileLauncher, in AbilityComponent abilityComponent)
            {
                ref var settings = ref projectileLauncher.Settings.Value;
                if (projectileLauncher.LastLaunchTime + settings.LaunchInterval <= CurrentElapsedTime)
                {
                    // A projectile can be launched if a suitable target is found.
                    Entity projectileTarget = Entity.Null;
                    float3 targetPosition = float3.zero;
                    bool isEnemy = EnemyCharacterComponentLookup.HasComponent(abilityComponent.Owner);
                    LocalTransform abilityOwnerLocalTransform = LocalTransformLookup[abilityComponent.Owner];
                    if (isEnemy)
                    {
                        // This is an enemy, we only need to check the player.
                        float sqrDistance = math.distancesq(DamageReceiversContainer.PlayerDamageReceiver.Value.Position, abilityOwnerLocalTransform.Position);
                        if (sqrDistance <= settings.MinimalSqrDistanceToTarget)
                        {
                            projectileTarget = DamageReceiversContainer.PlayerDamageReceiver.Value.Entity;
                            targetPosition = DamageReceiversContainer.PlayerDamageReceiver.Value.Position;
                        }
                    }
                    else
                    {
                        // This is the player, we need to check all the enemies.
                        float smallestSqrDistance = float.MaxValue;
                        foreach (var enemyDamageReceiver in DamageReceiversContainer.EnemyDamageReceivers)
                        {
                            float sqrDistance = math.distancesq(enemyDamageReceiver.Position, abilityOwnerLocalTransform.Position);
                            if (sqrDistance <= settings.MinimalSqrDistanceToTarget && sqrDistance < smallestSqrDistance)
                            {
                                smallestSqrDistance = sqrDistance;
                                projectileTarget = enemyDamageReceiver.Entity;
                                targetPosition = enemyDamageReceiver.Position;
                            }
                        }
                    }

                    if (projectileTarget != Entity.Null)
                    {
                        // Launch the projectile
                        var projectileInstance = Ecb.Instantiate(projectileLauncher.ProjectilePrefab);
                        if (isEnemy)
                        {
                            Ecb.AddComponent<DamagesToPlayer>(projectileInstance);
                        }
                        else
                        {
                            Ecb.AddComponent<DamagesToEnemy>(projectileInstance);
                        }

                        // TODO_IMPROVEMENT: add this offset in a settings or as a constant.
                        float3 projectileInitialPosition = abilityOwnerLocalTransform.Position + new float3(0f, 0.5f, 0f);
                        float scale = LocalTransformLookup[projectileLauncher.ProjectilePrefab].Scale;
                        Ecb.SetComponent(projectileInstance, new LocalTransform()
                        {
                            Position = projectileInitialPosition,
                            Rotation = abilityOwnerLocalTransform.Rotation,
                            Scale = scale
                        });

                        Ecb.SetComponent(projectileInstance, new Projectile()
                        {
                            Velocity =  math.normalizesafe(targetPosition - abilityOwnerLocalTransform.Position, float3.zero) * settings.InitialVelocity
                        });

                        Ecb.AddComponent(projectileInstance, new DamageDealer()
                        {
                            Damages = settings.Damages
                        });
                        
                        projectileLauncher.LastLaunchTime = CurrentElapsedTime;
                    }
                }
            }
        }
    }
}