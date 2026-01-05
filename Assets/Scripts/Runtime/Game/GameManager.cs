namespace Survivor.Runtime.Game
{
    using UnityEngine;
    using Unity.Entities;

    // TODO_IMPROVEMENT: should probably be a singleton of a plain C# class. But this is good enough for now as we have only one scene.
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int _targetFps = 60;

        [SerializeField] private float _fixedDeltaTime = 0.01666f;

        private void Start()
        {
            Application.targetFrameRate = _targetFps;

            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Timestep = _fixedDeltaTime;
        }
    }
}