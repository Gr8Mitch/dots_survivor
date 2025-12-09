namespace Survivor.Runtime.Enemy
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Transforms;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Controller;
    
    /// <summary>
    /// A system that makes the enemies move towards the player.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    partial struct MoveToPlayerSystem : ISystem
    {
        private EntityQuery _enemiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _enemiesQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CharacterController>()
                .WithAll<EnemyCharacterComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(_enemiesQuery);
            state.RequireForUpdate<AvatarCharacterComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity playerEntity = SystemAPI.GetSingletonEntity<AvatarCharacterComponent>();

            new MoveToPlayerJob()
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                PlayerEntity = playerEntity
            }.ScheduleParallel(_enemiesQuery);
        }

        [BurstCompile]
        public partial struct MoveToPlayerJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            public Entity PlayerEntity;

            /// <summary>
            ///  The position of the player, computed once for each chunk.
            /// </summary>
            private float3 _playerPosition;

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                _playerPosition = LocalTransformLookup[PlayerEntity].Position;

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }

            public void Execute(ref CharacterController characterController, in LocalTransform localTransform)
            {
                float3 deltaPosition = _playerPosition - localTransform.Position;
                characterController.Movement = math.normalizesafe(new float3(deltaPosition.x, 0f, deltaPosition.z));
            }
        }
    }
}