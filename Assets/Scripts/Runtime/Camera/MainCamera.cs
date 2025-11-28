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