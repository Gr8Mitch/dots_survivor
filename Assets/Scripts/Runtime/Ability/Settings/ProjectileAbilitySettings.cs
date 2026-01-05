namespace Survivor.Runtime.Ability
{
    using UnityEngine;
    using Unity.Entities;
    using Survivor.Runtime.Projectiles;

    /// <summary>
    /// The <see cref="IAbilitySettings"/> related to an ability that periodically launch projectiles.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAbilitySettings", menuName = "Survivor/Abilities/ProjectileAbilitySettings")]
    public class ProjectileAbilitySettings : IAbilitySettings
    {
        [Header("Projectile")]
        
        [Tooltip("The projectile prefab to instantiate.")]
        [SerializeField]
        private GameObject _projectilePrefab;

        [Tooltip("The damages inflicted by the projectile.")]
        [SerializeField]
        public ushort _damages = 1;
        
        [Tooltip("The minimum time interval between two projectiles launches (in seconds).")]
        [SerializeField]
        private float _launchInterval = 1f;
        
        [Tooltip( "The initial velocity of the projectile (in m/s)." )]
        [SerializeField]
        private float _initialVelocity = 10f;
        
        [Tooltip("The minimal distance to the target (in meters) to launch a projectile.")]
        [SerializeField]
        private float _minimalDistanceToTarget = 10f;

        //TODO : we can probably change this with some flags for some settings (like target closest or shoot at random, etc...)
        
        public override void Bake(Entity abilityEntity, AbilitiesContainerAuthoring.AbilitiesContainerAuthoringBaker baker)
        {
            base.Bake(abilityEntity, baker);

            if (_projectilePrefab == null)
            {
                Debug.LogError($"The projectile prefab of {this.name} is not set.");
                return;
            }
            
            // Bake the projectile prefab. We can't override the default settings of the projectile here. It will be done
            // when launching.
            Entity projectilePrefab = baker.GetEntity(_projectilePrefab, TransformUsageFlags.Dynamic);
            
            baker.AddComponent(abilityEntity, new ProjectileLauncher()
            {
                ProjectilePrefab = projectilePrefab,
                LastLaunchTime = 0.0,
                Damages = _damages,
                InitialVelocity = _initialVelocity,
                LaunchInterval = _launchInterval,
                MinimalSqrDistanceToTarget = _minimalDistanceToTarget * _minimalDistanceToTarget
            });
        }
    }
}