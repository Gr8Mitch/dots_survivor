namespace Survivor.Runtime.Game
{
    using UnityEngine;

    // TODO : should be a singleton of a plain C# class. But this is good enough for now as we have only one scene.
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int _targetFps = 60;

        void Start()
        {
            Application.targetFrameRate = _targetFps;
        }
    }
}