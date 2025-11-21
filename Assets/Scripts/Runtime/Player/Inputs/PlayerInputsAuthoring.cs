namespace Survivor.Runtime.Player.Inputs
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    
    /// <summary>
    /// Contains the player inputs gathered from the <see cref="InputAction"/>
    /// </summary>
    public struct PlayerInputs : IComponentData
    {
        public float2 movement; 
    }

    public class PlayerInputsAuthoring : MonoBehaviour
    {
        public class PlayerInputsAuthoringBaker : Baker<PlayerInputsAuthoring>
        {
            public override void Bake(PlayerInputsAuthoring authoring)
            {
                // No need to add a transform here, it is a different entity from the avatar.
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<PlayerInputs>(entity);
            }
        }
    }
}



