namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Contains the prefabs linked to the vfxs
    /// </summary>
    public struct VfxPrefabsContainer : IComponentData
    {
        public Entity NumbersVfxDigitPrefab;
    }
    
    class VfxPrefabsContainerAuthoring : MonoBehaviour
    {
        public GameObject NumbersVfxDigitPrefab;
        
        class VfxPrefabsContainerAuthoringBaker : Baker<VfxPrefabsContainerAuthoring>
        {
            public override void Bake(VfxPrefabsContainerAuthoring authoring)
            {
                var containerEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(containerEntity, new VfxPrefabsContainer()
                {
                    NumbersVfxDigitPrefab = GetEntity(authoring.NumbersVfxDigitPrefab, TransformUsageFlags.Renderable)
                });
            }
        }
    }
}