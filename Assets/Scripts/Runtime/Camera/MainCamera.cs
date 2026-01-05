namespace Survivor.Runtime.Camera
{
    using UnityEngine;

    // TODO_IMPROVEMENT: make something better to make it cleaner. Like registering itself to SyncCameraSystem or something like that.
    /// <summary>
    /// A pseudo singleton to get the main camera.
    /// Used by <see cref="SyncCameraSystem"/> to sync the transform of the entity representing the camera with the real camera.
    /// </summary>
    public class MainCamera : MonoBehaviour
    {
        public static Camera Instance { get; private set; }
        
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSingleton()
        {
            Instance = null;
        }
#endif //UNITY_EDITOR

        private void Awake()
        {
            Instance = GetComponent<Camera>();
        }
        
        private void OnDestroy()
        {
            Instance = null;
        }
    }
}