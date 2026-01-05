namespace Survivor.Runtime.Lifecycle
{
    using Unity.Entities;
    
    // TODO_IMPROVEMENT: also add PendingDestruction to all the entities of the LinkedEntityGroup so that we can destroy the entities
    // directly through the EntityQuery? Maybe it would be much faster.
    /// <summary>
    /// A tag component to mark a entity as dead, waiting to be destroyed.
    /// </summary>
    public struct PendingDestruction : IComponentData { }

    /// <summary>
    /// Entities with this component will be destroyed after a specific elapsed time.
    /// </summary>
    public struct LimitedLifetime : IComponentData
    {
        /// <summary>
        /// The remaining lifetime before the entity is destroyed.
        /// </summary>
        public double RemainingLifetime;
    }
}