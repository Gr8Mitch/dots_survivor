namespace Survivor.Runtime.Lifecycle
{
    using Unity.Entities;
    using UnityEngine;
    
    /// <summary>
    /// An authoring component to create anything that deals damages (including the enemies dealing damages to the player
    /// when touching it).
    /// </summary>
    class DamageDealerAuthoring : MonoBehaviour
    {
        // TODO_EDITOR: display only the relevant data according to the DamageType.
        public ushort Damages;
        [Tooltip("If true, the damages will be applied to the player. Otherwise, to the enemies.")]
        public bool DamagePlayer = false;
        public DamageType DamagesType = DamageType.DestroyOnDamage;
        public float SphereZoneRadius = 1f;
        public float CooldownDuration = 0.5f;
        
        private void OnValidate()
        {
            Damages = (ushort)Mathf.Clamp(Damages, 1, ushort.MaxValue);
        }
        
        class DamageDealerAuthoringBaker : Baker<DamageDealerAuthoring>
        {
            public override void Bake(DamageDealerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DamageDealer()
                {
                    Damages = authoring.Damages,
                });

                if (authoring.DamagePlayer)
                {
                    AddComponent(entity, new DamagesToPlayer());
                }
                else
                {
                    AddComponent(entity, new DamagesToEnemy());
                }

                if (authoring.DamagesType.HasFlag(DamageType.DestroyOnDamage))
                {
                    AddComponent(entity, new DestroyOnDamage());
                }
                
                if (authoring.DamagesType.HasFlag(DamageType.SphereDamageZone))
                {
                    AddComponent(entity, new DamageSphereZone()
                    {
                        Radius = authoring.SphereZoneRadius
                    });
                    AddComponent(entity, new DamageCooldown()
                    {
                        CooldownDuration = authoring.CooldownDuration
                    });
                }
                
                if (authoring.DamagesType.HasFlag(DamageType.TargetedDamages))
                {
                    AddComponent(entity, new TargetedDamages());
                }
            }
        }
    }
}