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

    /// <summary>
    /// Handles the character movements, including the ground detection.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerAvatarMovementSystem))]
    [BurstCompile]
    partial struct CharacterControllerSystem : ISystem
    {
        private struct ControllerTransientData
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<ColliderCastHit> ColliderCastHits;
            
            [NativeDisableContainerSafetyRestriction]
            public NativeList<RaycastHit> RaycastHits;

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
            }
        }
        
        private CollisionFilter _castToEnvironmentCollisionFilter;
        private ControllerTransientData _transientData;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _castToEnvironmentCollisionFilter = PhysicsUtilities.BuildFilterForRaycast(LayerConstants.ENVIRONMENT_LAYER);
            _transientData = new ControllerTransientData()
            {
                ColliderCastHits = default
            };
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            new HandleCharacterMovementsJob()
            {
                PhysicsWorld = physicsWorld,
                CastToEnvironmentCollisionFilter = _castToEnvironmentCollisionFilter,
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                TransientData = _transientData
            }.ScheduleParallel();
        }

        // TODO: do a IJobChunk to avoid the allocation of the collider cast hits in OnChunkBegin, as it seems buggy.
        // It would also be easier to handle optional components.
        [BurstCompile]
        private partial struct HandleCharacterMovementsJob : IJobEntity, IJobEntityChunkBeginEnd
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
            
            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                TransientData.AllocateContainers();

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted) { }
            
            public unsafe void Execute(Entity characterEntity,
                ref LocalTransform localTransform, 
                ref CharacterBodyData characterBodyData, 
                in CharacterController characterController,
                in PhysicsCollider characterPhysicsCollider,
                in CharacterComponent characterComponent)
            {
                ref CharacterSettings settings = ref characterComponent.Settings.Value;
                bool isInterpolating = settings.UseGroundInterpolation 
                                       && (ElapsedTime - characterBodyData.LastGroundCastTime) < settings.GroundInterpolationDuration;

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
                            in characterPhysicsCollider,
                            in characterComponent,
                            CastToEnvironmentCollisionFilter,
                            TransientData.ColliderCastHits);
                    }
                    else
                    {
                        CharacterControllerUtilities.ComputeGroundRaycast(ref localTransform, 
                            ref characterBodyData, 
                            ref PhysicsWorld, 
                            in characterPhysicsCollider,
                            in characterComponent,
                            CastToEnvironmentCollisionFilter,
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

                // Check if there is any collision in the direction of the movement with a cast. If so, snap it to the collision point.
                CharacterControllerUtilities.ComputeHitsMovement(ref localTransform, 
                    ref characterBodyData, 
                    ref PhysicsWorld, 
                    in characterPhysicsCollider,
                    in characterComponent,
                    CastToEnvironmentCollisionFilter,
                    DeltaTime);
                
                // Rotate the character to face the direction of the movement.
                if (math.lengthsq(characterController.Movement) > 0f)
                {
                    CharacterControllerUtilities.SlerpRotationTowardsDirectionAroundUp(ref localTransform.Rotation, DeltaTime, 
                        math.normalizesafe(characterController.Movement), MathUtilities.GetUpFromRotation(localTransform.Rotation), settings.RotationSharpness);
                }

                if (!isInterpolating)
                {
                    characterBodyData.LastGroundCastTime = ElapsedTime;
                }
            }
        }
    }
}