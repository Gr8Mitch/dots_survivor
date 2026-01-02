namespace Survivor.Runtime.Ability
{
    using Unity.Entities;
    using UnityEngine;
    using Unity.Transforms;

    /// <summary>
    /// Contains the prefab entity used to manage abilities.
    /// </summary>
    public struct AbilityPrefab : IBufferElementData
    {
        public Entity Value;
    }
    
    public class AbilitiesContainerAuthoring : MonoBehaviour
    {
        [Tooltip("Contains all the abilities settings.")]
        public IAbilitySettings[] AbilitiesSettings;
        
        public class AbilitiesContainerAuthoringBaker : Baker<AbilitiesContainerAuthoring>
        {
            public override void Bake(AbilitiesContainerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var prefabsBuffer = AddBuffer<AbilityPrefab>(entity);
                
                foreach (var abilitySetting in authoring.AbilitiesSettings)
                {
                    if (abilitySetting != null)
                    {
                        var abilityEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                        AddComponent(abilityEntity, new AbilityComponent()
                        {
                            AbilityId = abilitySetting.AbilityId
                        });
                        
                        AddComponent<Prefab>(abilityEntity);
                        
                        // Abilities are always children entities.
                        AddComponent<Parent>(abilityEntity);
                        
                        abilitySetting.Bake(abilityEntity, this);

                        prefabsBuffer.Add(new AbilityPrefab()
                        {
                            Value = abilityEntity
                        });
                    }
                    else
                    {
                        Debug.LogError("Null IAbilitySettings in AbilitiesContainerAuthoring");
                    }
                }
            }
        }
    }
}