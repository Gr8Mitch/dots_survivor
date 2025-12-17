namespace Survivor.Runtime.Lifecycle
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Mathematics;

    // TODO : also handle regen.
    /// <summary>
    /// Updates the health of the entities on which damages where done.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamagesSystem))]
    [BurstCompile]
    partial struct HealthSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamagesContainer>();
            state.RequireForUpdate<HealthComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damagesContainer = SystemAPI.GetSingleton<DamagesContainer>();

            new UpdateHealthJob()
            {
                DamagesPerEntity = damagesContainer.DamagesPerEntity
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateHealthJob : IJobEntity
        {
            [ReadOnly] 
            public NativeParallelMultiHashMap<Entity, DamagesContainer.DamageData> DamagesPerEntity;

            private void Execute(Entity entity, ref HealthComponent healthComponent)
            {
                if (DamagesPerEntity.TryGetFirstValue(entity, out DamagesContainer.DamageData damagesData, out var iterator))
                {
                    // TODO: handle vfx when taking damage (numbers + blinking ?)
                    healthComponent.HitPoints = (ushort)math.max(0, healthComponent.HitPoints - damagesData.Damages);
                    while (DamagesPerEntity.TryGetNextValue(out damagesData, ref iterator))
                    {
                        healthComponent.HitPoints = (ushort)math.max(0, healthComponent.HitPoints - damagesData.Damages);
                    }

                    if (healthComponent.HitPoints == 0)
                    {
                        //TODO: handle death.
                    }
                }
            }
        }
    }
}