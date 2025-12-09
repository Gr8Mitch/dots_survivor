namespace Survivor.Runtime.Character
{
    using Unity.Entities;
    using Unity.Burst;
    using Unity.Physics.Systems;
    using Unity.Transforms;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public struct CharacterInterpolationSingleton : IComponentData
    {
        /// <summary>
        /// Represents the duration (s) of an interpolation between two fixed updates
        /// </summary>
        public float InterpolationDeltaTime;
        
        /// <summary>
        /// Represents the elapsed time when we last saved the positions the characters should be interpolating from.
        /// </summary>
        public double SavedInterpolationDataElapsedTime;
    }
    
    /// <summary>
    /// Stores the transform from the last fixed update.
    /// </summary>
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct UpdateCharacterInterpolationDataSystem : ISystem
    {
        private EntityQuery _interpolatedCharactersQuery;
        
        public void OnCreate(ref SystemState state)
        {
            _interpolatedCharactersQuery = SystemAPI.QueryBuilder().WithAllRW<CharacterInterpolationData>()
                .WithAll<LocalTransform>().Build();

            state.EntityManager.CreateSingleton(new CharacterInterpolationSingleton(), new FixedString64Bytes(nameof(CharacterInterpolationSingleton)));
            
            state.RequireForUpdate(_interpolatedCharactersQuery);
            state.RequireForUpdate<CharacterInterpolationSingleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref CharacterInterpolationSingleton singleton = ref SystemAPI.GetSingletonRW<CharacterInterpolationSingleton>().ValueRW;
            singleton.InterpolationDeltaTime = SystemAPI.Time.DeltaTime;
            singleton.SavedInterpolationDataElapsedTime = SystemAPI.Time.ElapsedTime;
            
             var job = new CharacterInterpolationDataUpdateJob()
             {
                 LocalTransformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true),
                 CharacterInterpolationDataTypeHandle = SystemAPI.GetComponentTypeHandle<CharacterInterpolationData>(false)
             };

            state.Dependency = job.ScheduleParallel(_interpolatedCharactersQuery, state.Dependency);
        }
        
        [BurstCompile]
        public unsafe struct CharacterInterpolationDataUpdateJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> LocalTransformTypeHandle;
            
            public ComponentTypeHandle<CharacterInterpolationData> CharacterInterpolationDataTypeHandle;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var localTransformPtr = chunk.GetComponentDataPtrRO(ref LocalTransformTypeHandle);
                var characterInterpolationDataPtr = chunk.GetComponentDataPtrRW(ref CharacterInterpolationDataTypeHandle);
                
                int chunkCount = chunk.Count;
                int sizeCharacterInterpolation = UnsafeUtility.SizeOf<CharacterInterpolationData>();
                var sizeTransform = UnsafeUtility.SizeOf<LocalTransform>();
                int sizePosition = UnsafeUtility.SizeOf<float3>();
                
                UnsafeUtility.MemCpyStride(
                    characterInterpolationDataPtr,
                    sizeCharacterInterpolation,
                    localTransformPtr,
                    sizeTransform,
                    sizePosition,
                    chunkCount
                );
            }
        }
    }

    /// <summary>
    /// Updates the position of the characters with the interpolated position.
    /// </summary>
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(LocalToWorldSystem))]
    [BurstCompile]
    public partial struct InterpolateCharactersSystem : ISystem
    {
        private EntityQuery _interpolatedCharactersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _interpolatedCharactersQuery = SystemAPI.QueryBuilder()
                .WithAll<CharacterInterpolationData>()
                .Build();
            
            state.RequireForUpdate(_interpolatedCharactersQuery);
            state.RequireForUpdate<CharacterInterpolationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //state.CompleteDependency();
            var characterInterpolationSingleton =
                SystemAPI.GetSingletonRW<CharacterInterpolationSingleton>();
            if (characterInterpolationSingleton.ValueRO.SavedInterpolationDataElapsedTime < 0 ||
                characterInterpolationSingleton.ValueRO.InterpolationDeltaTime == 0f)
            {
                return;
            }
            
            float timeAheadOfLastFixedUpdate = (float)(SystemAPI.Time.ElapsedTime - characterInterpolationSingleton.ValueRO.SavedInterpolationDataElapsedTime);
            float normalizedTimeAhead = math.clamp(timeAheadOfLastFixedUpdate / characterInterpolationSingleton.ValueRO.InterpolationDeltaTime, 0f, 1f);
            
            new InterpolateCharactersJob()
            {
                NormalizedTimeAhead = normalizedTimeAhead
            }.ScheduleParallel();
        }

        /// <summary>
        /// Updates the localToWorld of the characters with the interpolated position.
        /// Interpolates the positon between the one that was saved after the physics step before it was updated
        /// by the character controller, and the one that was computed by the character controller.
        /// </summary>
        [BurstCompile]
        public partial struct InterpolateCharactersJob : IJobEntity
        {
            /// <summary>
            /// Ratio representing how far in time we are in-between two fixed updates.
            /// </summary>
            public float NormalizedTimeAhead;
            
            public void Execute(ref LocalToWorld localToWorld, in CharacterInterpolationData interpolationData, in LocalTransform localTransform)
            {
                float3 interpolatedPosition = math.lerp(interpolationData.Position, localTransform.Position, NormalizedTimeAhead);
                localToWorld.Value = float4x4.TRS(interpolatedPosition, localTransform.Rotation, new float3(localTransform.Scale));
            }
        }
    }
}