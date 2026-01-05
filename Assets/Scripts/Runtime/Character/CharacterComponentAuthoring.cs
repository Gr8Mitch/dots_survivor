namespace Survivor.Runtime.Character
{
    using Survivor.Runtime.Maths;
    using Unity.Mathematics;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Collections;
    using UnityEngine.Serialization;

    /// <summary>
    /// Contain most of the settings of the character for the character controller.
    /// Meant to be blobified.
    /// </summary>
    public struct CharacterSettings
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
        
        /// <summary>
        /// True if we interpolate the position of the character on the ground, instead of using a cast.
        /// </summary>
        public bool UseGroundInterpolation;

        /// <summary>
        /// The time (in s) between casts
        /// </summary>
        public float GroundInterpolationDuration;

        public bool UseRaycastsForGround;
    }
    
    /// <summary>
    /// Component to add to the player entity and the enemies entities (for now).
    /// </summary>
    public struct CharacterComponent : IComponentData
    {
        public BlobAssetReference<CharacterSettings> Settings;
    }
    
    public class CharacterComponentAuthoring : MonoBehaviour
    {
        // TODO: put some settings in a scriptable object common to all characters or enemies?
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

        [Tooltip("True if we interpolate the position of the character on the ground, instead of using a cast.")]
        public bool UseGroundInterpolation = false;

        [Tooltip("The time (in s) between casts.")]
        public float GroundInterpolationDuration = 0.2f;
        
        [FormerlySerializedAs("UseRaycasts")] [Tooltip("True to use raycasts instead of collider casts for the ground computation.")]
        public bool UseRaycastsForGround = false;

        [Tooltip("True if we should inteporlate the position computed during the fixed update.")]
        public bool InterpolatePosition = true;
        
        class Baker : Baker<CharacterComponentAuthoring>
        {
            public override void Bake(CharacterComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                // Create the blob asset
                var builder = new BlobBuilder(Allocator.Temp);
                ref CharacterSettings characterSettings = ref builder.ConstructRoot<CharacterSettings>();
                
                characterSettings.MovementMaxSpeed = authoring.MovementMaxSpeed;
                characterSettings.GroundedMovementSharpness = authoring.GroundedMovementSharpness;
                characterSettings.GroundSnappingDistance = authoring.GroundSnappingDistance;
                characterSettings.MaxGroundedSlopeDotProduct = MathUtilities.AngleRadiansToDotRatio(math.radians(authoring.MaxGroundedSlopeAngle));
                characterSettings.RotationSharpness = authoring.RotationSharpness;
                characterSettings.UseGroundInterpolation = authoring.UseGroundInterpolation;
                characterSettings.GroundInterpolationDuration = authoring.GroundInterpolationDuration;
                characterSettings.UseRaycastsForGround = authoring.UseRaycastsForGround;
                
                var blobReference =
                    builder.CreateBlobAssetReference<CharacterSettings>(Allocator.Persistent);
                
                builder.Dispose();

                // Register the Blob Asset to the Baker for de-duplication and reverting.
                AddBlobAsset<CharacterSettings>(ref blobReference, out var hash);
                
                // Add the component
                AddComponent(entity, new CharacterComponent()
                {
                    Settings = blobReference
                });
                
                AddComponent(entity, new CharacterBodyData()
                {
                    LastGroundCastTime = float.MinValue
                });

                if (authoring.InterpolatePosition)
                {
                    AddComponent(entity, new CharacterInterpolationData());
                }
            }
        }
    }
}