namespace Survivor.Runtime.Projectiles
{
    using Unity.Entities;
    using UnityEngine;
    
    class ProjectileAuthoring : MonoBehaviour
    {
        class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Projectile());
            }
        }
    }
}