namespace Survivor.Runtime.Camera
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// A tag component to place on a entity which transform will be copied to the real camera.
    /// </summary>
    public struct CameraEntity : IComponentData { }
    
    class CameraEntityAuthoring : MonoBehaviour
    {
        class CameraEntityAuthoringBaker : Baker<CameraEntityAuthoring>
        {
            public override void Bake(CameraEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraEntity());
            }
        }
    }
}