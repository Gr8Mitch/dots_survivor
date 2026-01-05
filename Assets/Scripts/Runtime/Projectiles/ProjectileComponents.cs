namespace Survivor.Runtime.Projectiles
{
    using Unity.Entities;
    using Unity.Mathematics;
    
    /// <summary>
    /// A component to add on the projectile entity.
    /// </summary>
    public struct Projectile : IComponentData
    {
        /// <summary>
        /// The current velocity of the projectile (m/s) in world coordinates.
        /// </summary>
        public float3 Velocity;
    }

    /// <summary>
    /// Makes projectile launched at a periodic interval. Added on the ability entity.
    /// </summary>
    public struct ProjectileLauncher : IComponentData
    {
        /// <summary>
        /// The projectile prefab to instantiate.
        /// </summary>
        public Entity ProjectilePrefab;
        
        /// <summary>
        /// The elapsed time corresponding to the last projectile launch (seconds)
        /// </summary>
        public double LastLaunchTime;
        
        // TODO_IMPROVEMENT : make it a blob so that all these settings take less space and are shared.
        /// <summary>
        /// The damages inflicted by the projectile.
        /// </summary>
        public ushort Damages;
        
        /// <summary>
        /// The initial velocity of the projectile (in m/s).
        /// </summary>
        public float InitialVelocity;
        
        /// <summary>
        /// The minimum time interval between two projectiles launches (in seconds).
        /// </summary>
        public float LaunchInterval;
        
        /// <summary>
        /// The minimal distance to the target (in meters) to launch a projectile.
        /// </summary>
        public float MinimalSqrDistanceToTarget;
    }
}