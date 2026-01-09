namespace Survivor.Runtime.Ability
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// The (character) entity that owns this ability.
    /// </summary>
    public struct AbilityComponent : IComponentData
    {
        public AbilityId AbilityId;
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
        public AbilityId AbilityId;
    }
    
    /// <summary>
    /// Contains all the abilities that can be used by the owner.
    /// </summary>
    class OwnedAbilitiesAuthoring : MonoBehaviour
    {
        [Tooltip("The ids of the abilities that can be used by the owner")]
        [AbilityIdReference]
        public AbilityId[] AbilitiesIds;
        
        class OwnedAbilitiesAuthoringBaker : Baker<OwnedAbilitiesAuthoring>
        {
            public override void Bake(OwnedAbilitiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<OwnedAbility>(entity);
                if (authoring.AbilitiesIds != null)
                {
                    var pendingAbilities = AddBuffer<PendingAbility>(entity);
                    foreach (AbilityId abilityId in authoring.AbilitiesIds)
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
}

