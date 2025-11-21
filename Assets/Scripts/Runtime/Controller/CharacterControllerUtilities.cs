using Survivor.Runtime.Maths;
using Unity.Collections;

namespace Survivor.Runtime.Controller
{
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

        /// <summary>
        /// Checks if the character is grounded and snap it to the ground.
        /// </summary>
        /// <param name="localTransform"> Contains the position and rotation of the character. The position is modified if snapped to the ground. </param>
        /// <param name="characterBodyData"> Used to updated the grounded status and the corresponding hit. </param>
        /// <param name="physicsWorld"> Used to do the collider cast. </param>
        /// <param name="characterPhysicsCollider"> Used to generate the ColliderCastInput. </param>
        /// <param name="characterComponent"> Contains some settings, like the cast distance. </param>
        /// <param name="castToEnvironmentCollisionFilter"> The collision filter to use for the collider cast. </param>
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
                
            // Make a copy of the collider to change its collision filter while keeping its form.
            if (characterPhysicsCollider.Value.Value.Type != ColliderType.Capsule)
            {
                UnityEngine.Debug.LogError("The character collider is not a capsule collider, characters should have only one character for now.");
                return;
            }
                
            CapsuleCollider* originalCollider = (CapsuleCollider*)characterPhysicsCollider.Value.GetUnsafePtr();
            CapsuleCollider castCollider = new CapsuleCollider();
            castCollider.Initialize(originalCollider->Geometry, castToEnvironmentCollisionFilter, originalCollider->Material);
                
            // Cast collider to the ground to see if the character is grounded. Snap the position if necessary
            float castLength = characterComponent.GroundSnappingDistance;
            float3 castDirection = -groundUp;
            var colliderCastInput = new ColliderCastInput()
            {
                Collider = (Collider*)&castCollider,
                Start = characterPosition,
                End = characterPosition + castLength * castDirection,
                Orientation = characterRotation
            };
            colliderCastHits.Clear();
            AllHitsCollector<ColliderCastHit> collector = new AllHitsCollector<ColliderCastHit>(1f, ref colliderCastHits);
            var hasCollided = physicsWorld.CastCollider(colliderCastInput, ref collector);
            ColliderCastHit closestHit = default;
            closestHit.Fraction = float.MaxValue;
            
            foreach (var hit in collector.AllHits)
            {
                // ignore hits if we're going away from them
                float dotRatio = math.dot(hit.SurfaceNormal, castDirection);
                if (dotRatio < -math.EPSILON && IsGroundedOnSlopeNormal(characterComponent.MaxGroundedSlopeDotProduct, hit.SurfaceNormal, groundUp))
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
        }

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
            
            // Make a copy of the collider to change its collision filter while keeping its form.
            if (characterPhysicsCollider.Value.Value.Type != ColliderType.Capsule)
            {
                UnityEngine.Debug.LogError("The character collider is not a capsule collider, characters should have only one character for now.");
                return;
            }
            
            CapsuleCollider* originalCollider = (CapsuleCollider*)characterPhysicsCollider.Value.GetUnsafePtr();
            CapsuleCollider castCollider = new CapsuleCollider();
            castCollider.Initialize(originalCollider->Geometry, castToEnvironmentCollisionFilter, originalCollider->Material);
            ColliderCastInput castInput = new ColliderCastInput(characterPhysicsCollider.Value, castStart, castEnd, localTransform.Rotation);
            bool hasHitObstacle = physicsWorld.CastCollider(castInput, out ColliderCastHit hit);
            
            float remainingDistance = math.length(characterBodyData.Velocity) * deltaTime;
            float3 movementDirection = math.normalizesafe(characterBodyData.Velocity);
            if (hasHitObstacle)
            {
                float3 groundUp = CharacterControllerUtilities.GROUND_UP;
                bool isGroundedOnSlope = IsGroundedOnSlopeNormal(characterComponent.MaxGroundedSlopeDotProduct, hit.SurfaceNormal, groundUp);
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