using Unity.Mathematics;

namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;

    /// <summary>
    /// A component to add on the ability entity when they must move according to the character.
    /// </summary>
    public struct CharacterAbilityVfxOwnerComponent : IComponentData
    {
        //TODO: use flags to reduce the size of this struct if we add more bools.
        public bool ReplicateCharacterPosition;
        public bool AlignWithCharacterGround;
    }
    
    /// <summary>
    /// A component to add on the ability vfx entity when they must move according to the character.
    /// </summary>
    public struct CharacterAbilityVfxComponent : IComponentData
    {
        public Entity CharacterEntity;
        public float3 PositionOffset;
        //TODO: use flags to reduce the size of this struct if we add more bools.
        public bool ReplicateCharacterPosition;
        public bool AlignWithCharacterGround;
    }
}