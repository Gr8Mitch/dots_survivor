namespace Survivor.Runtime.Player
{
    using Unity.Mathematics;
    using Unity.Burst;
    using Unity.Entities;
    using Survivor.Runtime.Controller;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Player.Inputs;
    
    /// <summary>
    /// A system to bake the player avatar movement from the player inputs.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerInputsSystem))]
    [BurstCompile]
    partial struct PlayerAvatarMovementSystem : ISystem
    {
        private EntityQuery _playerCharacterControllerQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _playerCharacterControllerQuery = SystemAPI.QueryBuilder().WithAll<AvatarCharacterComponent>()
                .WithAllRW<CharacterController>().Build();
            
            state.RequireForUpdate<PlayerInputs>();
            state.RequireForUpdate(_playerCharacterControllerQuery);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ComputeMovementFromInputsJob()
            {
                PlayerInputs = SystemAPI.GetSingleton<PlayerInputs>()
            }.Schedule(_playerCharacterControllerQuery);
        }

        [BurstCompile]
        private partial struct ComputeMovementFromInputsJob : IJobEntity
        {
            public PlayerInputs PlayerInputs;
            
            public void Execute(ref CharacterController characterController)
            {
                // TODO: smooth it somehow?
                characterController.Movement = new float3(PlayerInputs.movement.x, 0.0f, PlayerInputs.movement.y);
                
                // TODO: when the camera will be able to rotate, use the rotation of the camera to compute the final movement.
            }
        }
    }
}