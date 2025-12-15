namespace Survivor.Runtime.Ability
{
    using UnityEngine;
    using Unity.Entities;
    
    public abstract class IAbilitySettings : ScriptableObject
    {
        [Tooltip("The unique id of the ability")]
        [SerializeField]
        private ushort _abilityId;
        
        [SerializeField]
        private string _debugName;
        
        public ushort AbilityId => _abilityId;
        
        /// <summary>
        /// Creates the components according to the type of ability.
        /// </summary>
        public abstract void Bake(Entity abilityEntity, AbilitiesContainerAuthoring.AbilitiesContainerAuthoringBaker baker);
    }
}
