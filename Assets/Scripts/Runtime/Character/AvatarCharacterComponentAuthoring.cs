namespace Survivor.Runtime.Character
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// A tag component to identify the avatar character.
    /// </summary>
    public struct AvatarCharacterComponent : IComponentData { }
    
    class AvatarCharacterComponentAuthoring : MonoBehaviour
    {
        class AvatarPawnComponentAuthoringBaker : Baker<AvatarCharacterComponentAuthoring>
        {
            public override void Bake(AvatarCharacterComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AvatarCharacterComponent());
            }
        }
    }
}