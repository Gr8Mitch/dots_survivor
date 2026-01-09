namespace Survivor.Runtime.Controller
{
    using Survivor.Runtime.Maths;
    using Unity.Collections;
    using Survivor.Runtime.Character;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Transforms;
    using CapsuleCollider = Unity.Physics.CapsuleCollider;
    using Unity.Entities;
    
    /// <summary>
    /// Contains all the colliders used for the ColliderCasts (with the relevant CollisionFilter)
    /// </summary>
    public struct CastCollidersContainer : IComponentData
    {
        public CastColliderData PlayerCastColliderData;
        public NativeHashMap<EnemyTypeId, CastColliderData> EnemiesCastColliderData;

        public void AllocateData(int enemiesCount, Allocator allocator)
        {
            EnemiesCastColliderData = new NativeHashMap<EnemyTypeId, CastColliderData>(enemiesCount, allocator);
        }

        public void Dispose()
        {
            PlayerCastColliderData.Dispose();

            foreach (var entry in EnemiesCastColliderData)
            {
                entry.Value.Dispose();
            }
            EnemiesCastColliderData.Dispose();
        }

        public CastColliderData GetColliderData(EnemyTypeId? enemyTypeId)
        {
            if (enemyTypeId == null)
            {
                return PlayerCastColliderData;
            }

            return EnemiesCastColliderData[enemyTypeId.Value];
        }
    }
    
    /// <summary>
    /// Contains some utility methods for the character controller.
    /// </summary>
    public static class CharacterControllerUtilities
    {
        #region Constants

        /// <summary>
        /// An offset added along the up axis to not actually collide with the ground.
        /// </summary>
        public const float GROUND_SNAP_OFFSET = 0.02f;
        
        public static readonly float3 GROUND_UP = new float3(0f, 1f, 0f);
        
        /// <summary>
        /// Offset value representing a desired distance to stay away from any collisions for the character
        /// </summary>
        public const float COLLISION_OFFSET = 0.02f;

        #endregion Constants
        
        #region Ground Detection
        /// <summary>
        /// Checks if the character is grounded and snap it to the ground.
        /// </summary>
        /// <param name="localTransform"> Contains the position and rotation of the character. The position is modified if snapped to the ground. </param>
        /// <param name="characterBodyData"> Used to updated the grounded status and the corresponding hit. </param>
        /// <param name="physicsWorld"> Used to do the collider cast. </param>
        /// <param name="characterPhysicsCollider"> Used to generate the ColliderCastInput. </param>
        /// <param name="characterComponent"> Contains some settings, like the cast distance. </param>
        /// <param name="castToEnvironmentCollisionFilter"> The collision filter to use for the collider cast. </param>
        /// <param name="colliderCastHits"> A list to store <see cref="ColliderCastHit"/>. </param>
        public static unsafe void ComputeGround(ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData,
            ref PhysicsWorld physicsWorld,
            in CharacterComponent characterComponent,
            NativeList<ColliderCastHit> colliderCastHits,
            in CastColliderData castColliderData)
        {
            quaternion characterRotation = localTransform.Rotation;
            ref float3 characterPosition = ref localTransform.Position;
            float3 groundUp = CharacterControllerUtilities.GROUND_UP;
            // Cast collider to the ground to see if the character is grounded. Snap the position if necessary
            ref CharacterSettings settings = ref characterComponent.Settings.Value;
            float castLength = settings.GroundSnappingDistance;
            float3 castDirection = -groundUp;

            float3 castStart = characterPosition;
            float3 castEnd = characterPosition + castLength * castDirection;

            ColliderCastInput colliderCastInput = new ColliderCastInput(castColliderData.GroundCastCollider, castStart, castEnd, characterRotation);
            colliderCastHits.Clear();
            AllHitsCollector<ColliderCastHit> collector = new AllHitsCollector<ColliderCastHit>(1f, ref colliderCastHits);
            bool hasCollided = false;
            physicsWorld.CastCollider(colliderCastInput, ref collector);
            ColliderCastHit closestHit = default;
            closestHit.Fraction = float.MaxValue;
            
            foreach (var hit in collector.AllHits)
            {
                // ignore hits if we're going away from them
                float dotRatio = math.dot(hit.SurfaceNormal, castDirection);
                if (dotRatio < -math.EPSILON && IsGroundedOnSlopeNormal(settings.MaxGroundedSlopeDotProduct, hit.SurfaceNormal, groundUp))
                {
                    if (hit.Fraction < closestHit.Fraction)
                    {
                        closestHit = hit;
                        hasCollided = true;
                    }
                }
            }
            
            characterBodyData.GroundHitData = new HitData(closestHit);
            characterBodyData.IsGrounded = hasCollided;
                
            if (hasCollided)
            {
                // Snap on the ground.
                float distanceToGround = closestHit.Fraction * castLength;
                characterPosition += (CharacterControllerUtilities.GROUND_SNAP_OFFSET - distanceToGround) * groundUp;
            }
            
            characterBodyData.LastGroundPosition = characterPosition;
        }
        
        public static unsafe void ComputeGroundRaycast(ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData,
            ref PhysicsWorld physicsWorld,
            in PhysicsCollider characterPhysicsCollider,
            in CharacterComponent characterComponent,
            in CollisionFilter castToEnvironmentCollisionFilter,
            NativeList<RaycastHit> raycastHits)
        {
            ref float3 characterPosition = ref localTransform.Position;
            float3 groundUp = CharacterControllerUtilities.GROUND_UP;
            // Cast collider to the ground to see if the character is grounded. Snap the position if necessary
            ref CharacterSettings settings = ref characterComponent.Settings.Value;
            float castLength = settings.GroundSnappingDistance;
            float3 castDirection = -groundUp;
            
            if (!GetColliderData(in characterPhysicsCollider, out float radius, out float colliderHalfHeight))
            {
                return;
            }
            // Add a little offset towards the character to handle low obstacles.
            // It kind of enables us to have the hits on the front of the collider when we move (that we have
            // by default when doing a collider cast) and make the character go up and not bump into low front obstacles.
            float forwardOffset = radius;
            float3 movementDirection = math.normalizesafe(characterBodyData.Velocity);
            float totalCastLength = 2 * colliderHalfHeight + castLength;
            float3 castStart = characterPosition + movementDirection * forwardOffset - colliderHalfHeight * castDirection;
            float3 castEnd = castStart + totalCastLength * castDirection;
            
            raycastHits.Clear();
            RaycastInput colliderCastInput = new RaycastInput()
            {
                Start = castStart,
                End = castEnd,
                Filter = castToEnvironmentCollisionFilter
            };
            AllHitsCollector<RaycastHit> collector = new AllHitsCollector<RaycastHit>(1f, ref raycastHits);
            var hasCollided = false;
            physicsWorld.CastRay(colliderCastInput, ref collector);
            RaycastHit closestHit = default;
            closestHit.Fraction = float.MaxValue;
            
            foreach (var hit in collector.AllHits)
            {
                // ignore hits if we're going away from them
                float dotRatio = math.dot(hit.SurfaceNormal, castDirection);
                if (dotRatio < -math.EPSILON && IsGroundedOnSlopeNormal(settings.MaxGroundedSlopeDotProduct, hit.SurfaceNormal, groundUp))
                {
                    if (hit.Fraction < closestHit.Fraction)
                    {
                        closestHit = hit;
                        hasCollided = true;
                    }
                }
            }
            
            characterBodyData.GroundHitData = new HitData(closestHit);
            characterBodyData.IsGrounded = hasCollided;
                
            if (hasCollided)
            {
                // Snap on the ground.
                float distanceToGround = closestHit.Fraction * totalCastLength - colliderHalfHeight - math.dot(movementDirection * forwardOffset, groundUp);
                characterPosition += (CharacterControllerUtilities.GROUND_SNAP_OFFSET - distanceToGround) * groundUp;
            }
            
            characterBodyData.LastGroundPosition = characterPosition;
        }

        public static void InterpolateGround(ref LocalTransform localTransform,
            ref CharacterBodyData characterBodyData,
            in PhysicsCollider characterPhysicsCollider,
            in CharacterComponent characterComponent)
        {
            // Nothing to do for now. We just keep the previous ground data.
            // The velocity will be oriented accordingly.
        }
        
        #endregion Ground Detection

        #region Hit Detection
        public static unsafe void ComputeHitsMovement(Entity colliderEntity,
            ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData,
            ref PhysicsWorld physicsWorld,
            in CharacterComponent characterComponent,
            float deltaTime,
            NativeList<ColliderCastHit> colliderCastHits,
            in CastColliderData castColliderData,
            out bool hasOverlaps)
        {
             // TODO: iterate multiple times to integrate all the collisions more accurately?
            hasOverlaps = false;
            float3 castStart = localTransform.Position;
            float remainingDistance = math.length(characterBodyData.Velocity) * deltaTime;
            float3 movementDirection = math.normalizesafe(characterBodyData.Velocity);
            float3 castEnd = localTransform.Position + movementDirection * (remainingDistance + COLLISION_OFFSET);
            
            ColliderCastInput colliderCastInput = new ColliderCastInput(castColliderData.ObstacleCastCollider, castStart, castEnd, localTransform.Rotation);
            colliderCastHits.Clear();
            AllHitsCollector<ColliderCastHit> collector = new AllHitsCollector<ColliderCastHit>(1f, ref colliderCastHits);
            bool hasHitObstacle = physicsWorld.CastCollider(colliderCastInput, ref collector);
            
            
            ColliderCastHit closestHit = default;
            closestHit.Fraction = float.MaxValue;
            float dotRatioOfSelectedHit = float.MaxValue;
            bool isClosestHitGroundedOnSlope = false;
            
            if (hasHitObstacle)
            {
                float3 groundUp = CharacterControllerUtilities.GROUND_UP;
                ref CharacterSettings settings = ref characterComponent.Settings.Value;
                foreach (var hit in collector.AllHits)
                {
                    if (colliderEntity == hit.Entity)
                    {
                        continue;
                    }
                    
                    bool isGroundedOnSlope = IsGroundedOnSlopeNormal(settings.MaxGroundedSlopeDotProduct,
                        hit.SurfaceNormal, groundUp);
                    float dotRatio = math.dot(hit.SurfaceNormal, movementDirection);
                    if (dotRatio < -math.EPSILON && hit.Fraction <= closestHit.Fraction)
                    {
                        if (hit.Fraction < closestHit.Fraction || dotRatio < dotRatioOfSelectedHit)
                        {
                            // This is either a slope or the obstacle opposes the velocity.
                            closestHit = hit;
                            dotRatioOfSelectedHit = dotRatio;
                            isClosestHitGroundedOnSlope = isGroundedOnSlope;
                            hasOverlaps |= hit.Fraction <= 0f;
                        }
                    }
                }

                if (closestHit.Fraction < float.MaxValue)
                {
                    if (isClosestHitGroundedOnSlope)
                    {
                        characterBodyData.Velocity =
                            MathUtilities.ReorientVectorOnPlaneAlongDirection(characterBodyData.Velocity,
                                closestHit.SurfaceNormal, groundUp);
                    }
                    else
                    {
                        // The obstacle opposes the velocity.
                        float distanceToHit = math.max(0, remainingDistance * closestHit.Fraction);
                        remainingDistance -= distanceToHit;
                        localTransform.Position += movementDirection * distanceToHit;

                        // Project the velocity on the hits
                        float3 groundedCreaseDirection =
                            math.normalizesafe(
                                math.cross(characterBodyData.GroundHitData.Normal, closestHit.SurfaceNormal));
                        characterBodyData.Velocity =
                            math.projectsafe(characterBodyData.Velocity, groundedCreaseDirection);
                        movementDirection = math.normalizesafe(characterBodyData.Velocity);
                    }
                }
            }

            if (remainingDistance > 0f)
            {
                localTransform.Position += movementDirection * remainingDistance;
            }
        }
        
        
        // Not used for now because not accurate enough, but may be useful later.
        public static unsafe void ComputeHitsMovementRaycast(ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData,
            ref PhysicsWorld physicsWorld,
            in PhysicsCollider characterPhysicsCollider,
            in CharacterComponent characterComponent,
            CollisionFilter castToEnvironmentCollisionFilter,
            float deltaTime)
        {
            if (!GetColliderData(in characterPhysicsCollider, out float colliderRadius, out float colliderHalfHeight))
            {
                return;
            }
            
            // TODO: iterate multiple times to integrate all the collisions more accurately?
            float remainingDistance = math.length(characterBodyData.Velocity) * deltaTime;
            float3 movementDirection = math.normalizesafe(characterBodyData.Velocity);
            float3 transformUp = math.mul(localTransform.Rotation, math.up());
            float3 transformForward = math.mul(localTransform.Rotation, math.forward());
            float forwardDistance = 0.01f;
            float rayLength = remainingDistance + 2 * colliderRadius;
            float3 castStart = localTransform.Position + transformUp * colliderHalfHeight - movementDirection * colliderRadius;
            float3 castEnd = castStart + movementDirection * rayLength + transformForward * forwardDistance;
            RaycastInput castInput = new RaycastInput()
            {
                Start = castStart,
                End = castEnd,
                Filter = castToEnvironmentCollisionFilter
            };
            
            bool hasHitObstacle = physicsWorld.CastRay(castInput, out RaycastHit hit);
            
            if (hasHitObstacle)
            {
                float3 groundUp = CharacterControllerUtilities.GROUND_UP;
                ref CharacterSettings settings = ref characterComponent.Settings.Value;
                bool isGroundedOnSlope = IsGroundedOnSlopeNormal(settings.MaxGroundedSlopeDotProduct, hit.SurfaceNormal, groundUp);
                if (isGroundedOnSlope)
                {
                    characterBodyData.Velocity = MathUtilities.ReorientVectorOnPlaneAlongDirection(characterBodyData.Velocity, hit.SurfaceNormal, groundUp);
                }
                else if (math.dot(hit.SurfaceNormal, characterBodyData.Velocity) < math.EPSILON)
                {
                    float distanceToHit = math.dot(-hit.SurfaceNormal, movementDirection) *
                                          (rayLength * hit.Fraction - 2 * colliderRadius);
                    remainingDistance -= distanceToHit;
                    localTransform.Position += movementDirection * distanceToHit;

                    // Project the velocity on the hits
                    float3 groundedCreaseDirection =
                        math.normalizesafe(math.cross(characterBodyData.GroundHitData.Normal, hit.SurfaceNormal));
                    characterBodyData.Velocity = math.projectsafe(characterBodyData.Velocity, groundedCreaseDirection);
                    movementDirection = math.normalizesafe(characterBodyData.Velocity);
                }
            }
            
            localTransform.Position += movementDirection * remainingDistance;
        }
        
        #endregion Hit Detection

        #region Overlaps

        /// <summary>
        /// Solves the overlaps between the characters and/or the environment using Distance queries.
        /// </summary>
        public static void SolveOverlaps(Entity characterEntity, 
            ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData, 
            ref PhysicsWorld physicsWorld, 
            in CharacterComponent characterComponent, 
            NativeList<DistanceHit> transientDataDistanceHits, 
            in CastColliderData castColliderData)
        {
            // TODO: for now don't do anything if not ground. Should we change that?
            if (!characterBodyData.IsGrounded)
            {
                return;
            }
            
            ColliderDistanceInput distanceInput = new ColliderDistanceInput(castColliderData.ObstacleCastCollider, 0f, math.RigidTransform(localTransform.Rotation, localTransform.Position), localTransform.Scale);
            transientDataDistanceHits.Clear();
            AllHitsCollector<DistanceHit> collector = new AllHitsCollector<DistanceHit>(distanceInput.MaxDistance, ref transientDataDistanceHits);
            physicsWorld.CalculateDistance(distanceInput, ref collector);

            DistanceHit closestHit = default;
            closestHit.Fraction = float.MaxValue;
            
            for (int i = 0; i < collector.NumHits; ++i)
            {
                var hit = collector.AllHits[i];
                if (hit.Entity == characterEntity)
                {
                    continue;
                }

                if (hit.Distance < closestHit.Distance)
                {
                    closestHit = hit;
                }
            }

            if (closestHit.Distance != float.MaxValue)
            {
                float decollisionDistance = - closestHit.Distance;
                float3 decollisionDirection = closestHit.SurfaceNormal;
                float3 decollisionVector = decollisionDirection * decollisionDistance;
                
                decollisionDirection = math.normalizesafe(MathUtilities.ProjectOnPlane(decollisionDirection, characterBodyData.GroundHitData.Normal));
                RecalculateDecollisionVector(ref decollisionVector, closestHit.SurfaceNormal, decollisionDirection, decollisionDistance);
                
                localTransform.Position += decollisionVector;
            }
        }

        #endregion Overlaps

        /// <summary>
        /// Generate a copy of the collider passed in parameter with a different collision filter.
        /// </summary>
        /// <returns> False if the collider type is not handled, true otherwise. </returns>
        public static unsafe bool GenerateCastCollider(in PhysicsCollider collider, in CollisionFilter collisionFilter, out BlobAssetReference<Collider> castCollider)
        {
            if (collider.Value.Value.Type == ColliderType.Capsule)
            {
                CapsuleCollider* originalCollider = (CapsuleCollider*)collider.Value.GetUnsafePtr();
                castCollider = CapsuleCollider.Create(originalCollider->Geometry, collisionFilter);
            }
            else if (collider.Value.Value.Type == ColliderType.Sphere)
            {
                SphereCollider* originalCollider = (SphereCollider*)collider.Value.GetUnsafePtr();
                castCollider = SphereCollider.Create(originalCollider->Geometry, collisionFilter);
            }
            else
            {
                UnityEngine.Debug.LogError("The character collider is not a capsule collider nor a sphere colliders, characters should have only one collider for now (no compound colliders).");
                castCollider = default;
                return false;
            }

            return true;
        }

        private static unsafe bool GetColliderData(in PhysicsCollider collider, out float colliderRadius, out float colliderHalfHeight)
        {

            if (collider.Value.Value.Type == ColliderType.Capsule)
            {
                CapsuleCollider* originalCollider = (CapsuleCollider*)collider.Value.GetUnsafePtr();
                colliderRadius = originalCollider->Geometry.Radius;
                colliderHalfHeight = originalCollider->Geometry.GetHeight() * 0.5f;
            }
            else if (collider.Value.Value.Type == ColliderType.Sphere)
            {
                SphereCollider* originalCollider = (SphereCollider*)collider.Value.GetUnsafePtr();
                colliderRadius = originalCollider->Geometry.Radius;
                colliderHalfHeight = colliderRadius;
            }
            else
            {
                UnityEngine.Debug.LogError("The character collider is not a capsule collider nor a sphere colliders, characters should have only one collider for now (no compound colliders).");
                colliderRadius = 0f;
                colliderHalfHeight = 0f;
                return false;
            }

            return true;
        }
        
        public static void InterpolateGroundMovement(ref float3 currentVelocity, float3 targetVelocity, float sharpness, float deltaTime,
            float3 groundUp, float3 groundHitNormal)
        {
            // Reorients the current velocity on the current ground plane. 
            currentVelocity = MathUtilities.ReorientVectorOnPlaneAlongDirection(currentVelocity, groundHitNormal, groundUp);
            targetVelocity = MathUtilities.ReorientVectorOnPlaneAlongDirection(targetVelocity, groundHitNormal, groundUp);
            InterpolateVelocityTowardsTarget(ref currentVelocity, targetVelocity, deltaTime, sharpness);
        }
        
        public static void InterpolateVelocityTowardsTarget(ref float3 velocity, float3 targetVelocity, float deltaTime, float interpolationSharpness)
        {
            velocity = math.lerp(velocity, targetVelocity, MathUtilities.GetSharpnessInterpolant(interpolationSharpness, deltaTime));
        }
        
        /// <summary>
        /// Interpolates a rotation to make it face a direction, but constrains rotation to make it pivot around a designated up axis
        /// </summary>
        /// <param name="rotation"> The modified rotation </param>
        /// <param name="deltaTime"> The time delta </param>
        /// <param name="direction"> The direction to face </param>
        /// <param name="upDirection"> The rotation constraint up axis </param>
        /// <param name="orientationSharpness"> The sharpness of the rotation (how fast it interpolates) </param>
        public static void SlerpRotationTowardsDirectionAroundUp(ref quaternion rotation, float deltaTime, float3 direction, float3 upDirection, float orientationSharpness)
        {
            if (math.lengthsq(direction) > 0f)
            {
                rotation = math.slerp(rotation, MathUtilities.CreateRotationWithUpPriority(upDirection, direction), MathUtilities.GetSharpnessInterpolant(orientationSharpness, deltaTime));
            }
        }
        
        /// <summary>
        /// Determines if the slope angle is within grounded tolerance
        /// </summary>
        /// <param name="maxGroundedSlopeDotProduct"> Dot product between grounding up and maximum slope normal direction </param>
        /// <param name="slopeSurfaceNormal"> Evaluated slope normal </param>
        /// <param name="groundingUp"> Character's grounding up </param>
        /// <returns> Whether or not the character can be grounded on this slope </returns>
        public static bool IsGroundedOnSlopeNormal(
            float maxGroundedSlopeDotProduct,
            float3 slopeSurfaceNormal,
            float3 groundingUp)
        {
            return math.dot(groundingUp, slopeSurfaceNormal) > maxGroundedSlopeDotProduct;
        }
        
        private static void RecalculateDecollisionVector(ref float3 decollisionVector, float3 originalHitNormal, float3 newDecollisionDirection, float decollisionDistance)
        {
            float overlapDistance = math.max(decollisionDistance, 0f);
            if (overlapDistance > 0f)
            {
                decollisionVector = MathUtilities.ReverseProjectOnVector(originalHitNormal * overlapDistance, 
                    newDecollisionDirection, 
                    overlapDistance * MathUtilities.DEFAULT_REVERSE_PROJECTION_MAX_LENGTH_RATIO);
            }
        }
    }
}