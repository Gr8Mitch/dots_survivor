namespace Survivor.Runtime.Physics
{
    using UnityEngine;
    using Unity.Physics;

    public class PhysicsUtilities
    {
        #region Layers
        // All of this can be extracted from ColliderBakingSystem.ProduceCollisionFilter
        
        // Convert a single layer index to a bitmask
        public static uint LayerToMask(int layer)
        {
            return 1u << layer;
        }

        // Converts Unity's Layer Collision Matrix into a DOTS layer collision mask
        public static uint BuildCollidesWithMask(int layer)
        {
            uint mask = 0;

            for (int other = 0; other < 32; other++)
            {
                // Unity returns true if collisions are DISABLED — so invert!
                bool disabled = Physics.GetIgnoreLayerCollision(layer, other);

                if (!disabled)
                    mask |= 1u << other;
            }

            return mask;
        }

        public static CollisionFilter BuildFilterWithCollisionMatrix(int layer)
        {
            return new CollisionFilter
            {
                BelongsTo = LayerToMask(layer),
                CollidesWith = BuildCollidesWithMask(layer),
                GroupIndex = 0
            };
        }
        
        public static CollisionFilter BuildFilter(int layer)
        {
            return new CollisionFilter
            {
                BelongsTo = LayerToMask(layer),
                CollidesWith = LayerToMask(layer),
                GroupIndex = 0
            };
        }
        
        public static CollisionFilter BuildFilterForRaycast(int collidesWithLayer)
        {
            return new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = LayerToMask(collidesWithLayer),
                GroupIndex = 0
            };
        }
        
        #endregion Layers
    }
}