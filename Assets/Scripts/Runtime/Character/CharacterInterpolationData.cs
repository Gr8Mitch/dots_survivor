namespace Survivor.Runtime.Character
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Contains the data used to the interpolation of the character's transform (just the position for now).
    /// We need the write group so that the <see cref="LocaltoWorldSystem"/>
    /// does not overwrite the <see cref="LocalToWorld"/> component after <see cref="InterpolateCharactersSystem"/>."/>"/>
    /// </summary>
    [WriteGroup(typeof(LocalToWorld))]
    public struct CharacterInterpolationData : IComponentData
    {
        public float3 Position;
    }
}