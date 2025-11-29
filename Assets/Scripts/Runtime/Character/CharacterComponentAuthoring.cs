namespace Survivor.Runtime.Character
{
    using Survivor.Runtime.Maths;
    using Unity.Mathematics;
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// Component to add to the player entity and the enemies entities (for now).
    /// </summary>
    public struct CharacterComponent : IComponentData
    {
        /// <summary>
        /// The maximum speed of the character (m/s).
        /// </summary>
        public float MovementMaxSpeed;
        
        /// <summary>
        /// The sharpness of the ground movement interpolation.
        /// </summary>
        public float GroundedMovementSharpness;
        
        /// <summary>
        /// The distance (m) below which the character will snap to the ground. Also used as the length of the cast.
        /// </summary>
        public float GroundSnappingDistance;
        
        /// <summary>
        /// The dot prod corresponding to the max slope angle that the character can be considered grounded on.
        /// </summary>
        public float MaxGroundedSlopeDotProduct;
        
        /// <summary>
        /// The sharpness used for the rotation smoothing
        /// </summary>
        public float RotationSharpness;
    }
    
    public class CharacterComponentAuthoring : MonoBehaviour
    {
        [Tooltip("The maximum speed of the character (m/s).")]
        public float MovementMaxSpeed = 15f;

        [Tooltip("The distance (m) below which the character will snap to the ground. Also used as the length of the cast.")]
        public float GroundSnappingDistance = 0.5f;
        
        [Tooltip("The sharpness of the ground movement interpolation.")]
        public float GroundedMovementSharpness = 15f;
        
        [Tooltip("The max slope angle (°) that the character can be considered grounded on")]
        public float MaxGroundedSlopeAngle = 60f;

        [Tooltip("The sharpness used for the rotation smoothing.")]
        public float RotationSharpness = 25f;
        
        class Baker : Baker<CharacterComponentAuthoring>
        {
            public override void Bake(CharacterComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CharacterComponent()
                {
                    MovementMaxSpeed = authoring.MovementMaxSpeed,
                    GroundedMovementSharpness = authoring.GroundedMovementSharpness,
                    GroundSnappingDistance = authoring.GroundSnappingDistance,
                    MaxGroundedSlopeDotProduct = MathUtilities.AngleRadiansToDotRatio(math.radians(authoring.MaxGroundedSlopeAngle)),
                    RotationSharpness = authoring.RotationSharpness
                });
                AddComponent(entity, new CharacterBodyData());
            }
        }
    }
}