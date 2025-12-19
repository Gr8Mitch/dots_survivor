namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Contains the prefabs linked to the vfxs
    /// </summary>
    public struct DamageNumberVfxPrefabContainer : IComponentData
    {
        public Entity NumbersVfxDigitPrefab;
    }
    
    class DamageNumberVfxPrefabContainerAuthoring : MonoBehaviour
    {
        public GameObject NumbersVfxDigitPrefab;
        
        class DamageNumberVfxPrefabContainerAuthoringBaker : Baker<DamageNumberVfxPrefabContainerAuthoring>
        {
            public override void Bake(DamageNumberVfxPrefabContainerAuthoring authoring)
            {
                var containerEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(containerEntity, new DamageNumberVfxPrefabContainer()
                {
                    NumbersVfxDigitPrefab = GetEntity(authoring.NumbersVfxDigitPrefab, TransformUsageFlags.Renderable)
                });
            }
        }
    }
}