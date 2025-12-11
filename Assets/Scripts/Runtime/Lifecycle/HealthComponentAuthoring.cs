namespace Survivor.Runtime.Lifecycle
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Contains the current hit points of the entity and the radius of the hitbox.
    /// </summary>
    public struct HealthComponent : IComponentData
    {
        public float HitBoxRadius;
        public ushort HitPoints;
    }
    
    class HealthComponentAuthoring : MonoBehaviour
    {
        public float HitBoxRadius = 1f;
        public ushort InitialHitPoints;

        private void OnValidate()
        {
            InitialHitPoints = (ushort)Mathf.Clamp(InitialHitPoints, 1, ushort.MaxValue);
        }

        class HealthComponentAuthoringBaker : Baker<HealthComponentAuthoring>
        {
            public override void Bake(HealthComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HealthComponent()
                {
                    HitBoxRadius = authoring.HitBoxRadius,
                    HitPoints = authoring.InitialHitPoints
                });
            }
        }
    }
}