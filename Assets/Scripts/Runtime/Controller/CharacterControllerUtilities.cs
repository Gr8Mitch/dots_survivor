namespace Survivor.Runtime.Controller
{
    using Survivor.Runtime.Maths;
    using Unity.Collections;
    using Survivor.Runtime.Character;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Transforms;
    using CapsuleCollider = Unity.Physics.CapsuleCollider;
    using Collider = Unity.Physics.Collider;
    
    /// <summary>
    /// Contains some utility methods for the character controller.
    /// </summary>
    public static class CharacterControllerUtilities
    {
        #region Constants

        /// <summary>
        /// An offset added along the up axis to not actually collide with the ground.
        /// </summary>
        public const float GROUND_SNAP_OFFSET = 0.01f;
        
        public static readonly float3 GROUND_UP = new float3(0f, 1f, 0f);

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
            in PhysicsCollider characterPhysicsCollider,
            in CharacterComponent characterComponent,
            CollisionFilter castToEnvironmentCollisionFilter,
            NativeList<ColliderCastHit> colliderCastHits)
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

            if (!GenerateColliderCastInput(in characterPhysicsCollider,in castStart,in castEnd,
                    in castToEnvironmentCollisionFilter,in characterRotation, out ColliderCastInput colliderCastInput))
            {
                return;
            }
            
            colliderCastHits.Clear();
            AllHitsCollector<ColliderCastHit> collector = new AllHitsCollector<ColliderCastHit>(1f, ref colliderCastHits);
            var hasCollided = physicsWorld.CastCollider(colliderCastInput, ref collector);
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
            CollisionFilter castToEnvironmentCollisionFilter,
            NativeList<RaycastHit> raycastHits)
        {
            ref float3 characterPosition = ref localTransform.Position;
            float3 groundUp = CharacterControllerUtilities.GROUND_UP;
            // Cast collider to the ground to see if the character is grounded. Snap the position if necessary
            ref CharacterSettings settings = ref characterComponent.Settings.Value;
            float castLength = settings.GroundSnappingDistance;
            float3 castDirection = -groundUp;
            
            if (!GetColliderData(in characterPhysicsCollider, out _, out float colliderHalfHeight))
            {
                return;
            }
            
            float3 castStart = characterPosition - (castLength + colliderHalfHeight) * castDirection;
            float3 castEnd = characterPosition + (castLength + colliderHalfHeight) * castDirection;
            
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
                float distanceToGround = closestHit.Fraction * 2 * (castLength + colliderHalfHeight) - (castLength + colliderHalfHeight);
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
        public static unsafe void ComputeHitsMovement(ref LocalTransform localTransform, 
            ref CharacterBodyData characterBodyData,
            ref PhysicsWorld physicsWorld,
            in PhysicsCollider characterPhysicsCollider,
            in CharacterComponent characterComponent,
            CollisionFilter castToEnvironmentCollisionFilter,
            float deltaTime)
        {
             // TODO: iterate multiple times to integrate all the collisions more accurately?
            float3 castStart = localTransform.Position;
            float3 castEnd = localTransform.Position + characterBodyData.Velocity * deltaTime;
            
            if (!GenerateColliderCastInput(in characterPhysicsCollider,in castStart,in castEnd,
                    in castToEnvironmentCollisionFilter,in localTransform.Rotation, out ColliderCastInput castInput))
            {
                return;
            }
            
            bool hasHitObstacle = physicsWorld.CastCollider(castInput, out ColliderCastHit hit);
            
            float remainingDistance = math.length(characterBodyData.Velocity) * deltaTime;
            float3 movementDirection = math.normalizesafe(characterBodyData.Velocity);
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
                    float distanceToHit = remainingDistance * hit.Fraction;
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
        
        private static unsafe bool GenerateColliderCastInput(in PhysicsCollider collider, 
            in float3 castStart, in float3 castEnd, in CollisionFilter collisionFilter, in quaternion colliderRotation,
            out ColliderCastInput colliderCastInput)
        {
            // Make a copy of the collider to change its collision filter while keeping its form.
            if (collider.Value.Value.Type == ColliderType.Capsule)
            {
                CapsuleCollider* originalCollider = (CapsuleCollider*)collider.Value.GetUnsafePtr();
                CapsuleCollider castCollider = new CapsuleCollider();
                castCollider.Initialize(originalCollider->Geometry, collisionFilter, originalCollider->Material);
                colliderCastInput = new ColliderCastInput(collider.Value, castStart, castEnd, colliderRotation);
            }
            else if (collider.Value.Value.Type == ColliderType.Sphere)
            {
                SphereCollider* originalCollider = (SphereCollider*)collider.Value.GetUnsafePtr();
                SphereCollider castCollider = new SphereCollider();
                castCollider.Initialize(originalCollider->Geometry, collisionFilter, originalCollider->Material);
                colliderCastInput = new ColliderCastInput(collider.Value, castStart, castEnd, colliderRotation);
            }
            else
            {
                UnityEngine.Debug.LogError("The character collider is not a capsule collider nor a sphere colliders, characters should have only one collider for now (no compound colliders).");
                colliderCastInput = default;
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

    }
}