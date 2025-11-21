namespace Survivor.Runtime.Controller
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    
    /// <summary>
    /// A controller for the player and the enemies (for now at least).
    /// </summary>
    public struct CharacterController : IComponentData
    {
        // TODO : for now, it takes just the raw inputs for player. We will probably need to smooth them out later.
        public float3 Movement; 
    }
    
    public class CharacterControllerAuthoring : MonoBehaviour
    {
        class Baker : Baker<CharacterControllerAuthoring>
        {
            public override void Bake(CharacterControllerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CharacterController());
            }
        }
    }
}