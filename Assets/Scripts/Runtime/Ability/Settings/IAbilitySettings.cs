namespace Survivor.Runtime.Ability
{
    using UnityEngine;
    using Unity.Entities;
    using Survivor.Runtime.Vfx;
    
    public abstract class IAbilitySettings : ScriptableObject
    {
        [Header("General")]
        [Tooltip("The unique id of the ability")]
        [SerializeField]
        private ushort _abilityId;
        
        [SerializeField]
        private string _debugName;

        [Header("VFX Settings")]
        [SerializeField] 
        private VfxPrefabSettings _vfxPrefabSettings;
        
        // TODO_EDITOR: make it not visible in the inspector if _vfxPrefabSettings is null.
        [Tooltip("True if the VFX must replicate the position of the owner character.")]
        [SerializeField]
        private bool _replicateCharacterPosition = true;

        [Tooltip("True if the VFX must align with the ground normal of the owner character.")]
        [SerializeField]
        private bool _alignWithCharacterGround = true;

        public ushort AbilityId => _abilityId;

        /// <summary>
        /// Creates the components according to the type of ability.
        /// </summary>
        public virtual void Bake(Entity abilityEntity,
            AbilitiesContainerAuthoring.AbilitiesContainerAuthoringBaker baker)
        {
            if (_vfxPrefabSettings != null)
            {
                baker.AddComponent(abilityEntity, new VfxPrefabId()
                {
                    Value = _vfxPrefabSettings.ID
                });

                if (_alignWithCharacterGround || _replicateCharacterPosition)
                {
                    baker.AddComponent(abilityEntity, new CharacterAbilityVfxOwnerComponent()
                    {
                        ReplicateCharacterPosition = _replicateCharacterPosition,
                        AlignWithCharacterGround = _alignWithCharacterGround
                    });
                }
            }
        }
    }
}
