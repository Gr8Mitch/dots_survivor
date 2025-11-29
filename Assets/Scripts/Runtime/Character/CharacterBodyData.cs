namespace Survivor.Runtime.Character
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// A struct that can be built from all the hits types (<see cref="Unity.Physics.RaycastHit"/>, <see cref="Unity.Physics.ColliderCastHit"/>, etc.)
    /// </summary>
    public readonly struct HitData
    {
        public readonly Entity Entity;
        public readonly int RigidBodyIndex;
        public readonly float3 Position;
        public readonly float3 Normal;

        public HitData(Unity.Physics.RaycastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
        }

        public HitData(Unity.Physics.ColliderCastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
        }
    }

    /// <summary>
    /// Stores data that gets modified by the character controller during the update.
    /// </summary>
    public struct CharacterBodyData : IComponentData
    {
        public float3 Velocity;
        public HitData GroundHitData;
        public bool IsGrounded;
    }
}