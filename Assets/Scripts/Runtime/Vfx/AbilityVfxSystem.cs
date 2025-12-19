namespace Survivor.Runtime.Vfx
{
    using Unity.Burst;
    using Unity.Entities;
    using Survivor.Runtime.Character;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Transforms;

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    partial struct AbilityVfxSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterAbilityVfxComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Probably not worth parallelizing this job.
            new UpdateCharacterAbilitiesJob()
            {
                CharacterBodyDataLookup = SystemAPI.GetComponentLookup<CharacterBodyData>(true),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule();
        }

        [BurstCompile]
        private partial struct UpdateCharacterAbilitiesJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CharacterBodyData> CharacterBodyDataLookup;
            
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            
            private void Execute(ref LocalTransform localTransform,
                in CharacterAbilityVfxComponent characterAbilityVfxComponent)
            {
                if (characterAbilityVfxComponent.AlignWithCharacterGround)
                {
                    var characterBodyData = CharacterBodyDataLookup[characterAbilityVfxComponent.CharacterEntity];
                    localTransform.Rotation = quaternion.LookRotation(localTransform.Forward(), characterBodyData.GroundHitData.Normal);
                }
                
                if (characterAbilityVfxComponent.ReplicateCharacterPosition)
                {
                    var characterLocalToWorld = LocalToWorldLookup[characterAbilityVfxComponent.CharacterEntity];
                    localTransform.Position = characterLocalToWorld.Position + math.mul(localTransform.Rotation, characterAbilityVfxComponent.PositionOffset);
                }
            }
        }
    }
}