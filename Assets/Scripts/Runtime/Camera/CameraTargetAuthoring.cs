namespace Survivor.Runtime.Camera
{
    using Unity.Mathematics;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// A component to place on the player entity to make it the target of the camera.
    /// Also contains some settings for the camera.
    /// </summary>
    public struct CameraTarget : IComponentData
    {
        public float3 OffsetToTarget;
        public quaternion Rotation;
    }
    
    class CameraTargetAuthoring : MonoBehaviour
    {
        public Vector3 OffsetToTarget;
        public Vector3 EulerRotation;
        
        class CameraTargetAuthoringBaker : Baker<CameraTargetAuthoring>
        {
            public override void Bake(CameraTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraTarget()
                {
                    OffsetToTarget = authoring.OffsetToTarget,
                    Rotation = Quaternion.Euler(authoring.EulerRotation)
                });
            }
        }
    }
}