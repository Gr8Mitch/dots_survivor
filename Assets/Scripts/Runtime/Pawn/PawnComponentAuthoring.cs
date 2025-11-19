namespace Survivor.Runtime.Pawn
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// Component to add to the player entity and the enemies entities (for now).
    /// </summary>
    public struct PawnComponent : IComponentData { }
    
    public class PawnComponentAuthoring : MonoBehaviour
    {
        class Baker : Baker<PawnComponentAuthoring>
        {
            public override void Bake(PawnComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PawnComponent());
            }
        }
    }
}