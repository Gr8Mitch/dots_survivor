namespace Survivor.Runtime.Camera
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Syncs the camera gameobject transform with the target entity transform.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    partial class SyncCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            RequireForUpdate<CameraEntity>();
        }

        protected override void OnUpdate()
        {
            if (MainCamera.Instance != null)
            {
                Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<CameraEntity>();
                LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
                MainCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation);
            }
        }
    }

}