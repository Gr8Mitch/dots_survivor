namespace Survivor.Runtime.Ability
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Transforms;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Lifecycle;
    using Survivor.Runtime.Vfx;

    /// <summary>
    /// Creates the ability entities from the <see cref="PendingAbility"/> components.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    partial struct AbilitiesInitializationSystem : ISystem
    {
        private EntityQuery _pendingAbilitiesQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _pendingAbilitiesQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld>()
                .WithAllRW<PendingAbility, OwnedAbility>()
                .Build();
            
            state.RequireForUpdate<AbilityPrefab>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_pendingAbilitiesQuery.IsEmpty)
            {
                // This creates a sync point, but it should be ok at the start of the frame.
                return;
            }

            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var abilityPrefabs = SystemAPI.GetSingletonBuffer<AbilityPrefab>();

            // No need to parallelize this job, it would probably bring too much overhead.
            new InstantiateAbilityJob()
            {
                AbilityPrefabs = abilityPrefabs,
                AbilityComponentLookup = SystemAPI.GetComponentLookup<AbilityComponent>(true),
                AvatarCharacterComponentLookup = SystemAPI.GetComponentLookup<AvatarCharacterComponent>(true),
                Ecb = ecb
            }.Schedule();
        }

        [BurstCompile]
        private partial struct InstantiateAbilityJob : IJobEntity
        {
            [ReadOnly]
            public DynamicBuffer<AbilityPrefab> AbilityPrefabs;
            
            [ReadOnly]
            public ComponentLookup<AbilityComponent> AbilityComponentLookup;
            
            [ReadOnly]
            public ComponentLookup<AvatarCharacterComponent> AvatarCharacterComponentLookup;
            
            public EntityCommandBuffer Ecb;
            
            private void Execute(Entity abilityOwnerEntity, 
                ref DynamicBuffer<PendingAbility> pendingAbilities, 
                ref DynamicBuffer<OwnedAbility> ownedAbilities,
                in LocalToWorld localToWorld)
            {
                bool isAvatar = AvatarCharacterComponentLookup.HasComponent(abilityOwnerEntity);
                foreach (var pendingAbility in pendingAbilities)
                {
                    // Search for the ability prefab in the buffer.
                    foreach (var abilityPrefab in AbilityPrefabs)
                    {
                        if (AbilityComponentLookup[abilityPrefab.Value].AbilityId == pendingAbility.AbilityId)
                        {
                            Entity abilityInstance = Ecb.Instantiate(abilityPrefab.Value);
                            Ecb.SetName(abilityInstance, "Ability");
                            ownedAbilities.Add(new OwnedAbility()
                            {
                                AbilityEntity = abilityInstance
                            });

                            Ecb.SetComponent(abilityInstance, localToWorld);
                            Ecb.AddComponent(abilityInstance, new Parent()
                            {
                                Value = abilityOwnerEntity
                            });

                            if (isAvatar)
                            {
                                Ecb.AddComponent<DamagesToEnemy>(abilityInstance);
                            }
                            else
                            {
                                Ecb.AddComponent<DamagesToPlayer>(abilityInstance);
                            }
                            
                            Ecb.AppendToBuffer(abilityOwnerEntity, new OwnedAbility()
                            {
                                AbilityEntity = abilityInstance
                            });

                            Ecb.SetComponent(abilityInstance, new AbilityComponent()
                            {
                                AbilityId = pendingAbility.AbilityId,
                                Owner = abilityOwnerEntity
                            });
                            
                            // Make sure the entity is destroyed when the owner is destroyed.
                            Ecb.AppendToBuffer(abilityOwnerEntity, new LinkedEntityGroup()
                            {
                                Value = abilityInstance
                            });
                            
                            Ecb.AddComponent<VfxPrefabNotCreated>(abilityInstance);
                            
                            break;
                        }
                    }
                }
                
                pendingAbilities.Clear();
                Ecb.SetComponentEnabled<PendingAbility>(abilityOwnerEntity, false);
            }
        }
    }
}