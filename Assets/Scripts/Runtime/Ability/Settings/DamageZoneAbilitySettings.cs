namespace Survivor.Runtime.Ability
{
    using UnityEngine;
    using Unity.Entities;
    using Survivor.Runtime.Lifecycle;

    /// <summary>
    /// The <see cref="IAbilitySettings"/> related to an ability that periodically deals damage to entities in a zone.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageZoneAbilitySettings", menuName = "Survivor/Abilities/DamageZoneAbilitySettings")]
    public class DamageZoneAbilitySettings : IAbilitySettings
    {
        [Header("Damage Zone ability")]
        [SerializeField]
        private ushort _damages = 1;
        
        [SerializeField]
        private float _zoneRadius = 10f;
        
        /// <summary>
        /// The cooldown in seconds between damages.
        /// </summary>
        [SerializeField]
        private float _damagesCooldown = 1f;
        
        public override void Bake(Entity abilityEntity, AbilitiesContainerAuthoring.AbilitiesContainerAuthoringBaker baker)
        {
            base.Bake(abilityEntity, baker);
            
            baker.AddComponent(abilityEntity, new DamageSphereZone()
            {
                Radius = _zoneRadius
            });
            baker.AddComponent(abilityEntity, new DamageCooldown()
            {
                CooldownDuration = _damagesCooldown
            });
            baker.AddComponent(abilityEntity, new DamageDealer()
            {
                Damages = _damages
            });
        }

        private void OnValidate()
        {
            _damages = (ushort)Mathf.Clamp(_damages, 1, ushort.MaxValue);
            _zoneRadius = Mathf.Max(_zoneRadius, 0.01f);
            _damagesCooldown = Mathf.Max(_damagesCooldown, 0.01f);
        }
    }
}