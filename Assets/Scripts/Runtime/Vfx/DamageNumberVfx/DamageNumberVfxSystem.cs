namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;
    using Survivor.Runtime.Lifecycle;
    using Unity.Rendering;
    using Survivor.Runtime.Camera;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;
    using Unity.Burst.Intrinsics;

    public struct DamageNumberVfx : IComponentData
    {
        /// <summary>
        /// The elapsed time (seconds) when the vfx was created.
        /// </summary>
        public double CreationElaspedTime;
    }
    
    // TODO_IMPROVEMENT: It would probably be better to do most of the logic in the shaders. But it is probably good enough for now, 
    // as the current performances of all of this are quite good.
    // TODO_IMPROVEMENT:: add color and change alpha over time ?
    // TODO_IMPROVEMENT:: move it to the initialization group to prevent a sync point in EndSimulationEntityCommandBufferSystem ?
    
    /// <summary>
    /// Handles the "vfx" displaying the numbers showing the damages.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HealthSystem))]
    [BurstCompile]
    public partial struct DamageNumberVfxSystem : ISystem, ISystemStartStop
    {
        private EntityQuery _damageNumberVfxQuery;
        private Entity _numbersVfxPrefab;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamagesContainer>();
            state.RequireForUpdate<DamageNumberVfxPrefabContainer>();
            state.RequireForUpdate<CameraEntity>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _damageNumberVfxQuery = SystemAPI.QueryBuilder().WithAll<DamageNumberVfx>().Build();
        }
        
        public void OnStartRunning(ref SystemState state)
        {
            // Retrieve the number vfx prefab entity.
            
            // !!!! To make things work, we have to use a specific subscene, otherwise the
            // RenderMeshArray contains all the meshes and materials referenced by the subscene !!!!!
            // Because the prefab has multiple materials, it creates a LinkedEntityGroup with 11 entities (including a root).
            // The one with the index 1 is the 0 digit.
            var prefab = SystemAPI.GetSingleton<DamageNumberVfxPrefabContainer>().NumbersVfxDigitPrefab;
            var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(prefab);
            _numbersVfxPrefab = linkedEntityGroup[1].Value;
        }

        public void OnStopRunning(ref SystemState state) { }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damageContainers = SystemAPI.GetSingleton<DamagesContainer>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // We schedule the job even if there is no damages to display, as it is probably cheaper than creating
            // a sync point to check that the damages container is not empty.
            var cameraEntity = SystemAPI.GetSingletonEntity<CameraEntity>();
            var instantiateNumbersVfxJob = new InstantiateNumbersVfxJob()
            {
                Ecb = ecb,
                DamagesContainer = damageContainers,
                NumberPrefab = _numbersVfxPrefab,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                CameraEntity = cameraEntity
            };
            state.Dependency = instantiateNumbersVfxJob.Schedule(state.Dependency);

            if (_damageNumberVfxQuery.CalculateChunkCount() != 0)
            {
                // Move the vfx entities and destroy them if necessary
                // We probably don't need to parallelize this job, it would probably bring too much overhead.
                new UpdateVfxNumbers()
                {
                    ElapsedTime = SystemAPI.Time.ElapsedTime,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    Ecb = ecb
                }.Schedule();
            }
        }
        
        #region Jobs

        /// <summary>
        /// Creates the vfx entities for each dealt damages.
        /// </summary>
        [BurstCompile]
        private struct InstantiateNumbersVfxJob : IJob
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly]
            public DamagesContainer DamagesContainer;
            
            [ReadOnly]
            public Entity NumberPrefab;
            
            public double ElapsedTime;
            
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public Entity CameraEntity;

            public void Execute()
            {
                float3 cameraForward = LocalTransformLookup[CameraEntity].Forward();
                // Create a root with DamageNumberVfx + one child for each damage digit.
                foreach (var entry in DamagesContainer.DamagesPerEntity)
                {
                    Entity rootEntity = Ecb.CreateEntity();
                    Ecb.AddComponent(rootEntity, new DamageNumberVfx()
                    {
                        CreationElaspedTime = ElapsedTime
                    });
                    float4x4 rootMatrix = float4x4.TRS(entry.Value.Position, quaternion.identity, new float3(1f));
                    Ecb.AddComponent(rootEntity, new LocalToWorld()
                    {
                        Value = rootMatrix
                    });

                    // TODO: either use a constant or make it editable in a scriptable object or so.
                    float3 numbersPosition = entry.Value.Position + new float3(0f, 3f, 0f);
                    // TODO: should we use a specific quaternion for each digit? I guess one for the whole bunch is ok
                    quaternion vfxRotation = quaternion.LookRotation(cameraForward, math.up());
                    Ecb.AddComponent(rootEntity, new LocalTransform()
                    {
                        Position = numbersPosition,
                        Rotation = vfxRotation,
                        Scale =  1f
                    });

                    ushort damages = entry.Value.Damages;
                    int damagesDigits = (int)math.floor(math.log10(damages)) + 1;
                    const float offsetPerDigit = 0.5f;
                    float3 offset = new float3(-(damagesDigits - 1 ) * offsetPerDigit, 0f, 0f);
                    
                    for (int i = 0; i < damagesDigits; i++)
                    {
                        var numberEntity = Ecb.Instantiate(NumberPrefab);
                        Ecb.AddComponent(numberEntity, new Parent {Value = rootEntity});
                        Ecb.SetComponent(numberEntity, new LocalTransform()
                        {
                            Position = offset,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
                        int number = damages / (int)math.pow(10, damagesDigits - i - 1);
                        Ecb.SetComponent(numberEntity, MaterialMeshInfo.FromRenderMeshArrayIndices(number, 0));
                        
                        offset += new float3(offsetPerDigit, 0f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the position of the vfx entities, and kills them if they are too old.
        /// </summary>
        [BurstCompile]
        private partial struct UpdateVfxNumbers : IJobEntity
        {
            // TODO_IMPROVEMENT: make it editable in a scriptable object or so.
            private const float VFX_LIFETIME = 1f;
            private const float VFX_SPEED = 1f;
            
            public double ElapsedTime;
            public float DeltaTime;
            
            public EntityCommandBuffer Ecb;
            
            private void Execute(Entity entity, ref LocalTransform localTransform, in DamageNumberVfx damageNumberVfx, in DynamicBuffer<Child> childrenBuffer)
            {
                if (ElapsedTime - damageNumberVfx.CreationElaspedTime < VFX_LIFETIME)
                {
                    localTransform.Position += new float3(0f, VFX_SPEED * DeltaTime, 0f);
                    // No need to change the rotation regarding the billboard as long as the camera is not rotating.
                    
                    // TODO: change the alpha with time?
                }
                else
                {
                    // Destroy the vfx and its children. We could have used a LinkedEntityGroup here, but I guess it is good enough this way.
                    Ecb.DestroyEntity(entity);
                    foreach (var childEntity in childrenBuffer)
                    {
                        Ecb.DestroyEntity(childEntity.Value);
                    }
                }
            }
        }
        
        #endregion Jobs
    }
}