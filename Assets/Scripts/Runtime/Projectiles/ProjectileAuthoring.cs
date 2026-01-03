using Survivor.Runtime.Lifecycle;

namespace Survivor.Runtime.Projectiles
{
    using Unity.Entities;
    using UnityEngine;
    
    class ProjectileAuthoring : MonoBehaviour
    {
        [Tooltip("The lifetime of the projectile in seconds. Zero or negative if infinite.")]
        public float Lifetime = 20f;
        
        class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Projectile());
                if (authoring.Lifetime > 0f)
                {
                    AddComponent(entity, new LimitedLifetime()
                    {
                        RemainingLifetime = authoring.Lifetime
                    });
                }
            }
        }
    }
}