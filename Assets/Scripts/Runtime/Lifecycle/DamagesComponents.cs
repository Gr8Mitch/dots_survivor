namespace Survivor.Runtime.Lifecycle
{
    using System;
    using Unity.Entities;
    
    [Flags]
    public enum DamageType
    {
        DestroyOnDamage = 1 << 0,
        SphereDamageZone = 1 << 1,
        TargetedDamages = 1 << 2
    }
    
    /// <summary>
    /// The damage dealt by the entity. How are the damages are applied are linked to other components.
    /// </summary>
    public struct DamageDealer : IComponentData
    {
        public ushort Damages;
    }

    /// <summary>
    /// The damages are dealt periodically.
    /// </summary>
    public struct DamageCooldown : IComponentData
    {
        /// <summary>
        /// Cooldown duration (in seconds).
        /// </summary>
        public float CooldownDuration;
        
        /// <summary>
        /// Last elapsed time (in seconds) when damages were dealt.
        /// </summary>
        public double LastDamageDealtElapsedTime;

        public readonly bool IsCooldownOver(double elapsedTime)
        {
            return (elapsedTime - LastDamageDealtElapsedTime) > CooldownDuration;
        }

        public DamageCooldown OnDamagesDone(double elapsedTime)
        {
            return new DamageCooldown()
            {
                LastDamageDealtElapsedTime = elapsedTime,
                CooldownDuration = this.CooldownDuration
            };
        }
    }
    
    /// <summary>
    /// The damages are dealt to all characters in a spherical zone.
    /// </summary>
    public struct DamageSphereZone : IComponentData
    {
        public float Radius;
    }

    /// <summary>
    /// The damages are dealt to a specific target.
    /// </summary>
    public struct TargetedDamages : IComponentData
    {
        public Entity Target;
    }
    
    /// <summary>
    /// Tag component when the damages should be applied only to the player.
    /// </summary>
    public struct DamagesToPlayer : IComponentData { }
    
    /// <summary>
    /// Tag component when the damages should be applied only to the enemies.
    /// </summary>
    public struct DamagesToEnemy : IComponentData { }
    
    /// <summary>
    /// The entity should be destroyed when it inflicts damages.
    /// </summary>
    public struct DestroyOnDamage : IComponentData { }
}