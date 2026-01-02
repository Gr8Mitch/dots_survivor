namespace Survivor.Runtime.Controller
{
    using Survivor.Runtime.Maths;
    using Unity.Burst.Intrinsics;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Transforms;
    using Unity.Physics;
    using Unity.Mathematics;
    using Survivor.Runtime.Common;
    using Survivor.Runtime.Physics;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Player;
    using Unity.Collections.LowLevel.Unsafe;
    using Survivor.Runtime.Enemy;
    using Survivor.Runtime.Data;
    using Unity.Physics.Systems;
    
    /// <summary>
    /// Handles the character movements, including the ground detection.
    /// </summary>
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [BurstCompile]
    public partial struct CharacterControllerFixedUpdateSystem : ISystem, ISystemStartStop
    {
        /// <summary>
        /// Contains 
        /// </summary>
        private struct ControllerTransientData
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<ColliderCastHit> ColliderCastHits;
            
            [NativeDisableContainerSafetyRestriction]
            public NativeList<RaycastHit> RaycastHits;
            
            [NativeDisableContainerSafetyRestriction]
            public NativeList<DistanceHit> DistanceHits;

            public void AllocateContainers()
            {
                if (!ColliderCastHits.IsCreated)
                {
                    ColliderCastHits = new NativeList<ColliderCastHit>(8, Allocator.Temp);
                }
                
                if (!RaycastHits.IsCreated)
                {
                    RaycastHits = new NativeList<RaycastHit>(8, Allocator.Temp);
                }
                
                if (!DistanceHits.IsCreated)
                {
                    DistanceHits = new NativeList<DistanceHit>(8, Allocator.Temp);
                }
            }
        }
        
        private CollisionFilter _castToEnvironmentCollisionFilter;
        private CollisionFilter _castToObstacleCollisionFilter;
        private ControllerTransientData _transientData;
        private EntityQuery _characterControllersQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EnemyPrefabsContainer>();
            state.RequireForUpdate<AvatarPrefabContainer>();

            _characterControllersQuery = SystemAPI.QueryBuilder().WithAll<CharacterController>().Build();
            state.RequireAnyForUpdate(_characterControllersQuery);

            _castToEnvironmentCollisionFilter = PhysicsUtilities.BuildFilterForRaycast(LayerConstants.ENVIRONMENT_LAYER);
            _castToObstacleCollisionFilter = PhysicsUtilities.BuildFilterForRaycast(LayerConstants.ENVIRONMENT_LAYER, LayerConstants.ENEMY_LAYER, LayerConstants.PLAYER_LAYER);
            _transientData = new ControllerTransientData()
            {
                ColliderCastHits = default
            };
        }

        public void OnStartRunning(ref SystemState state)
        {
            // We have to create it here to make sure that we already have the prefabs container instantiated.
            // It is not symetric with the OnDestroy, but well it is good enough for the scope of the project.
            if (!SystemAPI.HasSingleton<CastCollidersContainer>())
            {
                CreateCollidersContainerSingleton(ref state);
            }
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<CastCollidersContainer>(out var castCollidersContainer))
            {
                castCollidersContainer.Dispose();
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<CastCollidersContainer>());
            }
            
            // No need to dispose the transient data as it is allocated with a Temp allocator by the chunks.
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            
            new HandleCharacterMovementsFixedUpdateJob()
            {
                PhysicsWorld = physicsWorld,
                CastToEnvironmentCollisionFilter = _castToEnvironmentCollisionFilter,
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                TransientData = _transientData,
                CastCollidersContainer = SystemAPI.GetSingleton<CastCollidersContainer>(),
                EnemyCharacterComponentLookup = SystemAPI.GetComponentLookup<EnemyCharacterComponent>(true)
            }.ScheduleParallel();
        }

        private void CreateCollidersContainerSingleton(ref SystemState state)
        {
            var enemyPrefabsContainer = SystemAPI.GetSingletonBuffer<EnemyPrefabsContainer>();
            var avatarPrefabContainer = SystemAPI.GetSingleton<AvatarPrefabContainer>();
            
            var castCollidersContainer = new CastCollidersContainer();
            castCollidersContainer.AllocateData(enemyPrefabsContainer.Length, Allocator.Persistent);
            var physicsColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(true);
            var enemyCharacterComponentLookup = SystemAPI.GetComponentLookup<EnemyCharacterComponent>(true);
            
            var avatarCollider = physicsColliderLookup[avatarPrefabContainer.PlayerAvatarPrefab];
            CharacterControllerUtilities.GenerateCastCollider(in avatarCollider, in _castToEnvironmentCollisionFilter, out var playerGroundCastCollider);
            CharacterControllerUtilities.GenerateCastCollider(in avatarCollider, in _castToObstacleCollisionFilter, out var playerObstacleCastCollider);
            castCollidersContainer.PlayerCastColliderData = new CastColliderData()
            {
                GroundCastCollider = playerGroundCastCollider,
                ObstacleCastCollider = playerObstacleCastCollider
            };
            
            for (int i = 0; i < enemyPrefabsContainer.Length; ++i)
            {
                var enemyCollider = physicsColliderLookup[enemyPrefabsContainer[i].EnemyPrefab];
                int enemyId = enemyCharacterComponentLookup[enemyPrefabsContainer[i].EnemyPrefab].EnemyTypeId;

                if (CharacterControllerUtilities.GenerateCastCollider(in enemyCollider,
                        in _castToEnvironmentCollisionFilter, out var enemyGroundCastCollider))
                {
                    // No need to check the 2nd GenerateCastCollider result as it will always be true here.
                    CharacterControllerUtilities.GenerateCastCollider(in enemyCollider, in _castToObstacleCollisionFilter, out var enemyObstacleCastCollider);
                    castCollidersContainer.EnemiesCastColliderData.Add(enemyId, new CastColliderData()
                    {
                        GroundCastCollider = enemyGroundCastCollider,
                        ObstacleCastCollider = enemyObstacleCastCollider
                    });
                }
            }

            state.EntityManager.CreateSingleton(castCollidersContainer);
        }
        
        [BurstCompile]
        private partial struct HandleCharacterMovementsFixedUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;

            [ReadOnly] 
            public CollisionFilter CastToEnvironmentCollisionFilter;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly] 
            public double ElapsedTime;

            /// <summary>
            /// Contains collider/raycast cast hit containers that are allocated once for each chunk.
            /// </summary>
            [NativeDisableParallelForRestriction]
            public ControllerTransientData TransientData;

            [ReadOnly]
            public CastCollidersContainer CastCollidersContainer;
            
            [ReadOnly]
            public ComponentLookup<EnemyCharacterComponent> EnemyCharacterComponentLookup;
            
            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                TransientData.AllocateContainers();

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted) { }
            
            public void Execute(Entity characterEntity,
                ref LocalTransform localTransform, 
                ref CharacterBodyData characterBodyData, 
                in CharacterController characterController,
                in PhysicsCollider characterPhysicsCollider,
                in CharacterComponent characterComponent)
            {
                ref CharacterSettings settings = ref characterComponent.Settings.Value;
                bool isInterpolating = settings.UseGroundInterpolation 
                                       && (ElapsedTime - characterBodyData.LastGroundCastTime) < settings.GroundInterpolationDuration;
        
                bool isEnemy =
                    EnemyCharacterComponentLookup.TryGetComponent(characterEntity, out var enemyCharacterComponent);
                // TODO: add these data in a component to avoid the random access?
                var castColliderData = CastCollidersContainer.GetColliderData(isEnemy ? enemyCharacterComponent.EnemyTypeId : null);
                
                if (isInterpolating)
                {
                    CharacterControllerUtilities.InterpolateGround(ref localTransform, 
                        ref characterBodyData, 
                        in characterPhysicsCollider,
                        in characterComponent);
                }
                else
                {
                    if (!settings.UseRaycastsForGround)
                    {
                        CharacterControllerUtilities.ComputeGround(ref localTransform, 
                            ref characterBodyData, 
                            ref PhysicsWorld, 
                            in characterComponent,
                            TransientData.ColliderCastHits,
                            in castColliderData);
                    }
                    else
                    {
                        CharacterControllerUtilities.ComputeGroundRaycast(ref localTransform, 
                            ref characterBodyData, 
                            ref PhysicsWorld, 
                            in characterPhysicsCollider,
                            in characterComponent,
                            in CastToEnvironmentCollisionFilter,
                            TransientData.RaycastHits);
                    }
                }
                
                // Update the velocity of the character. Project it on the ground normal.
                float3 groundUp = CharacterControllerUtilities.GROUND_UP;
                if (characterBodyData.IsGrounded)
                {
                    float3 targetVelocity =  settings.MovementMaxSpeed * characterController.Movement;
                    CharacterControllerUtilities.InterpolateGroundMovement(ref characterBodyData.Velocity, targetVelocity, settings.GroundedMovementSharpness, DeltaTime, groundUp, characterBodyData.GroundHitData.Normal);
                }
                else
                {
                    // Gravity
                    characterBodyData.Velocity += new float3(0f, -9.8f, 0f) * DeltaTime;
                }

                // Check if there is any collision in the direction of the movement with a cast. If so, snap it to the collision point.
                CharacterControllerUtilities.ComputeHitsMovement(characterEntity,
                    ref localTransform, 
                    ref characterBodyData, 
                    ref PhysicsWorld, 
                    in characterComponent,
                    DeltaTime,
                    TransientData.ColliderCastHits,
                    in castColliderData,
                    out bool hasOverlaps);

                if (hasOverlaps)
                {
                    CharacterControllerUtilities.SolveOverlaps(characterEntity,
                        ref localTransform,
                        ref characterBodyData,
                        ref PhysicsWorld,
                        in characterComponent,
                        TransientData.DistanceHits,
                        in castColliderData);
                }

                if (!isInterpolating)
                {
                    characterBodyData.LastGroundCastTime = ElapsedTime;
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct CharacterControllerVariableUpdateSystem : ISystem
    {
        private EntityQuery _characterControllersQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterControllersQuery = SystemAPI.QueryBuilder().WithAll<CharacterController, LocalTransform>().Build();
            state.RequireForUpdate(_characterControllersQuery);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new HandleCharacterMovementsVariableUpdateJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct HandleCharacterMovementsVariableUpdateJob : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;
            
            public void Execute(RefRW<LocalTransform> localTransform, 
                in CharacterController characterController, in CharacterComponent characterComponent)
            {
                // Rotate the character to face the direction of the movement.
                if (math.lengthsq(characterController.Movement) > 0f)
                {
                    ref CharacterSettings settings = ref characterComponent.Settings.Value;
                    quaternion rotation = localTransform.ValueRO.Rotation;
                    CharacterControllerUtilities.SlerpRotationTowardsDirectionAroundUp(ref rotation, DeltaTime, 
                        math.normalizesafe(characterController.Movement), MathUtilities.GetUpFromRotation(rotation), settings.RotationSharpness);
                    localTransform.ValueRW.Rotation = rotation;
                }
            }
        }
    }
}