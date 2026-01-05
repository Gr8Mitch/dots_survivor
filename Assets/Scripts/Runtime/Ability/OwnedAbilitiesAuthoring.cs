namespace Survivor.Runtime.Ability
{
    using Unity.Entities;
    using UnityEngine;
    
    // TODO_EDITOR: make it a specific type for the ability Id to be able to create a specific drawer and ensure its uniqueness.
    
    /// <summary>
    /// The (character) entity that owns this ability.
    /// </summary>
    public struct AbilityComponent : IComponentData
    {
        // TODO: Make it a specific type to be able to create a specific drawer and ensure its uniqueness.
        public ushort AbilityId;
        public Entity Owner;
    }

    /// <summary>
    /// The buffer contains all the owned abilities.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct OwnedAbility : IBufferElementData
    {
        public Entity AbilityEntity;
    }
    
    /// <summary>
    /// Contains all the abilities that needs to be instantiated.
    /// Should be disabled when empty, enabled otherwise.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct PendingAbility : IBufferElementData, IEnableableComponent
    {
        public ushort AbilityId;
    }
    
    /// <summary>
    /// Contains all the abilities that can be used by the owner.
    /// </summary>
    class OwnedAbilitiesAuthoring : MonoBehaviour
    {
        [Tooltip("The ids of the abilities that can be used by the owner")]
        public ushort[] AbilitiesIds;
        
        class OwnedAbilitiesAuthoringBaker : Baker<OwnedAbilitiesAuthoring>
        {
            public override void Bake(OwnedAbilitiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<OwnedAbility>(entity);
                var pendingAbilities = AddBuffer<PendingAbility>(entity);
                foreach (ushort abilityId in authoring.AbilitiesIds)
                {
                    pendingAbilities.Add(new PendingAbility()
                    {
                        AbilityId = abilityId
                    });
                }
            }
        }
    }
}

