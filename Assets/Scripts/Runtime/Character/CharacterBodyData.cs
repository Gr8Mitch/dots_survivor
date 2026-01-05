namespace Survivor.Runtime.Character
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;

    /// <summary>
    /// A struct that can be built from all the hits types (<see cref="Unity.Physics.RaycastHit"/>, <see cref="Unity.Physics.ColliderCastHit"/>, etc.)
    /// </summary>
    public readonly struct HitData
    {
        public static readonly HitData NULL = new HitData(Entity.Null);
        
        public readonly Entity Entity;
        
        // TODO_IMPROVEMENT: should we keep RigidBodyIndex, ColliderKey and Position? It does not seem to be read anywhere (for now).
        public readonly int RigidBodyIndex;
        /// <summary>
        /// Hit collider key
        /// </summary>
        public readonly ColliderKey ColliderKey;
        public readonly float3 Position;
        public readonly float3 Normal;

        private HitData(Entity entity)
        {
            Entity = entity;
            RigidBodyIndex = 0;
            ColliderKey = ColliderKey.Empty;
            Position = float3.zero;
            Normal = float3.zero;
        }
        
        public HitData(Unity.Physics.RaycastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
        }

        public HitData(Unity.Physics.ColliderCastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
        }
        
        public bool IsValid => Entity != Entity.Null;
    }

    /// <summary>
    /// Stores data that gets modified by the character controller during the update.
    /// </summary>
    public struct CharacterBodyData : IComponentData
    {
        public float3 Velocity;
        public HitData GroundHitData;
        public bool IsGrounded;
        
        // TODO_IMPROVEMENT: do we really need this? And/or shoud we move this somewhere else?
        public float3 LastGroundPosition;
        
        /// <summary>
        /// The last elapsed time when a cast was done.
        /// </summary>
        public double LastGroundCastTime;
    }
}