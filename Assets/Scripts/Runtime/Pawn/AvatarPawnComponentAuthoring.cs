namespace Survivor.Runtime.Pawn
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// A tag component to identify the avatar pawn.
    /// </summary>
    public struct AvatarPawnComponent : IComponentData { }
    
    class AvatarPawnComponentAuthoring : MonoBehaviour
    {
        class AvatarPawnComponentAuthoringBaker : Baker<AvatarPawnComponentAuthoring>
        {
            public override void Bake(AvatarPawnComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AvatarPawnComponent());
            }
        }
    }
}