using Unity.Transforms;

namespace Survivor.Runtime.Ability
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// The (character) entity that owns this ability.
    /// </summary>
    public struct AbilityComponent : IComponentData
    {
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
    
    class OwnedAbilitiesAuthoring : MonoBehaviour
    {
        // TODO : do a specific drawer.
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

