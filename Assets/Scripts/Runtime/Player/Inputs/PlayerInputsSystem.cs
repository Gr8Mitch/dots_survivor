namespace Survivor.Runtime.Player.Inputs
{
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Unity.Entities;
    
    /// <summary>
    /// Injects the player inputs gathered from the <see cref="InputAction"/> into the <see cref="PlayerInputs"/> component.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerInputsSystem : SystemBase
    {
        private const string InputActionName = "InputSystem_Actions";
        
        private InputActionAsset _actionAsset;
        private InputAction _moveAction;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            // We could have also loaded it from the addressables, but this is much simpler this way regarding the scope of the project.
            _actionAsset = Resources.Load<InputActionAsset>(InputActionName);
            if (_actionAsset == null)
            {
                Debug.LogError($"Input action {InputActionName} asset not found in the resources");
                return;
            }
            _actionAsset.Enable();
            _moveAction = _actionAsset.FindAction("Move");
            
            RequireForUpdate<PlayerInputs>();
        }

        protected override void OnUpdate()
        {
            if (_actionAsset == null)
            {
                return;
            }

            float2 movement = _moveAction.ReadValue<Vector2>();
            
            // PlayerInputs is a singleton as we only have one player.
            // We could have used a job to fill it, but as the system updates first, not using a job is fine, it should not 
            // create a sync point.
            var playerInputs = SystemAPI.GetSingletonRW<PlayerInputs>();
            playerInputs.ValueRW.movement = movement;
        }
    }
}