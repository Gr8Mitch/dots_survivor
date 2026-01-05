namespace Survivor.Runtime.Player
{
    using Unity.Mathematics;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
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
            var job = new ComputeMovementFromInputsJob()
            {
                PlayerInputs = SystemAPI.GetSingleton<PlayerInputs>()
            };
            job.Schedule(_playerCharacterControllerQuery);
        }

        [BurstCompile]
        private partial struct ComputeMovementFromInputsJob : IJobEntity
        {
            [ReadOnly]
            public PlayerInputs PlayerInputs;
            
            private void Execute(ref CharacterController characterController)
            {
                // TODO_IMPROVEMENT: smooth it somehow?
                characterController.Movement = new float3(PlayerInputs.movement.x, 0.0f, PlayerInputs.movement.y);
                
                // If the camera is ever able to rotate, use the rotation of the camera to compute the final movement.
            }
        }
    }
}