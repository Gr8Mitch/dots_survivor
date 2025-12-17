namespace Survivor.Runtime.Camera
{
    using UnityEngine;

    // TODO: make something better.
    /// <summary>
    /// A pseudo singleton to get the main camera.
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