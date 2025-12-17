using Unity.Burst.Intrinsics;

namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;
    using Survivor.Runtime.Lifecycle;
    using Unity.Rendering;
    using UnityEngine;
    using Survivor.Runtime.Camera;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine.Rendering;

    public struct DamageNumberVfx : IComponentData
    {
        public double CreationElaspedTime;
    }
    
    /// <summary>
    /// Handles the "vfx" displayig the numbers showing the damamges.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HealthSystem))]
    public partial class DamageNumberVfxSystem : SystemBase
    {
        private EntityQuery _damageNumberVfxQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();

            //CreateNumberEntityPrefab();
            
            RequireForUpdate<DamagesContainer>();
            RequireForUpdate<VfxPrefabsContainer>();
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _damageNumberVfxQuery = SystemAPI.QueryBuilder().WithAll<DamageNumberVfx>().Build();
            
            // Create a hierarchy of entities with the root being the container of the X digits, and the children being the individual digits.
            // All the digits share the same RenderMeshArray but not the MaterialMeshInfo
        }

        // protected override void OnStartRunning()
        // {
        //     base.OnStartRunning();
        //     
        //     // Modifies the RenderMeshArray of the digit prefab to include all the materials.
        //     // If it is done the baked prefab, it generates 10 sub entities.
        //     var numbersVfxDigitPrefab = SystemAPI.GetSingletonRW<VfxPrefabsContainer>().ValueRW.NumbersVfxDigitPrefab;
        //     var renderMeshArray = EntityManager.GetSharedComponentManaged<RenderMeshArray>(numbersVfxDigitPrefab);
        //     var mesh = renderMeshArray.MeshReferences[0];
        //     var material = renderMeshArray.MaterialReferences[0];
        //     EntityManager.SetSharedComponentManaged(numbersVfxDigitPrefab, CreateNumbersRenderMeshArray(mesh, material));
        // }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var damageContainers = SystemAPI.GetSingleton<DamagesContainer>();
            if (!damageContainers.DamagesPerEntity.IsEmpty)
            {
                // Create new VFX entities here.
                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged);
                
                // !!!! To make things work, we have to use a specific subscene, otherwise the
                // RenderMeshArray contains all the meshes and materials referenced by the subscene !!!!!
                // TODO: check if adding a section is enough on the global baking scene is enough to make it work.
                var prefab = SystemAPI.GetSingleton<VfxPrefabsContainer>().NumbersVfxDigitPrefab;
                // Because the prefab has multiple materials, it creates a LinkedEntityGroup with 11 entities (including a root).
                // The one with the index 1 is the 0 digit.
                var linkedEntityGroup = EntityManager.GetBuffer<LinkedEntityGroup>(prefab);
                var instantiateNumbersVfxJob = new InstantiateNumbersVfxJob()
                {
                    Ecb = ecb,
                    DamagesContainer = damageContainers,
                    NumberPrefab = linkedEntityGroup[1].Value,
                    ElapsedTime = SystemAPI.Time.ElapsedTime,
                    CameraPosition = MainCamera.Instance.transform.position,
                };
                Dependency = instantiateNumbersVfxJob.Schedule(Dependency);
            }

            if (_damageNumberVfxQuery.CalculateChunkCount() != 0)
            {
                // Move the vfx entities and destroy them if necessary
                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged);
                // We probably don't need to parallelize this job, it would probably bring too much overhead.
                new UpdateVfxNumbers()
                {
                    ElapsedTime = SystemAPI.Time.ElapsedTime,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    CameraPosition = MainCamera.Instance.transform.position,
                    Ecb = ecb
                }.Schedule();

                // TODO: move it to a specific system?
                // Also do the billboarding here.
                // Get the rotation of the camera via the MainCamera singleton or the CameraEntity singleton.

            }
        }

        /// <summary>
        /// Creates the prefab of the number entities.
        /// We don't use baking for now as it creates multiple entities if we add all the material variants on the same renderer.
        /// </summary>
        // private void CreateNumberEntityPrefab()
        // {
        //     // Create one common RenderMeshArray with the 10 materials, one with each damage digit.
        //     RenderMeshArray vfxRenderMeshArray = CreateNumbersRenderMeshArray();
        //     _numberEntityPrefab = EntityManager.CreateEntity(typeof(Prefab));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(LocalToWorld));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(LocalTransform));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(PreviousParent));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(Parent));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(RenderBounds));
        //     EntityManager.AddComponent(_numberEntityPrefab, typeof(MaterialMeshInfo));
        //     
        //     var renderMeshDescription =
        //         new RenderMeshDescription(ShadowCastingMode.Off, false, MotionVectorGenerationMode.ForceNoMotion);
        //     RenderMeshUtility.AddComponents(
        //         _numberEntityPrefab, 
        //         EntityManager, 
        //         renderMeshDescription, 
        //         vfxRenderMeshArray,
        //         MaterialMeshInfo.FromRenderMeshArrayIndices(0,0));
        // }
        
        // private RenderMeshArray CreateNumbersRenderMeshArray()
        // {
        //     Material originDamageVfxMaterial = Resources.Load<Material>("DamageVfxMaterial");
        //     Material[] damageVfxMaterials = new Material[10];
        //     for (int i = 0; i < 10; i++)
        //     {
        //         var material = new Material(originDamageVfxMaterial) { name = $"DamageVfxMaterial_{i}" };
        //         material.mainTextureOffset = new Vector2(i / 10f, 0f);
        //         damageVfxMaterials[i] = material;
        //     }
        //
        //     Mesh quadMesh = CreateQuadMesh(0.1f, 0.1f);
        //
        //     return new RenderMeshArray(damageVfxMaterials, new Mesh[] { quadMesh });
        // }
        
        private RenderMeshArray CreateNumbersRenderMeshArray(Mesh mesh, Material originDamageVfxMaterial)
        {
            Material[] damageVfxMaterials = new Material[10];
            for (int i = 0; i < 10; i++)
            {
                var material = new Material(originDamageVfxMaterial) { name = $"DamageVfxMaterial_{i}" };
                material.mainTextureOffset = new Vector2(i / 10f, 0f);
                damageVfxMaterials[i] = material;
            }

            return new RenderMeshArray(damageVfxMaterials, new Mesh[] { mesh });
        }

        private Mesh CreateQuadMesh(float width, float height)
        {
            // Picked from https://docs.unity3d.com/6000.3/Documentation/Manual/Example-CreatingaBillboardPlane.html
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            return mesh;
        }
        
        #region Jobs

        [BurstCompile]
        public struct InstantiateNumbersVfxJob : IJob
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly]
            public DamagesContainer DamagesContainer;
            
            [ReadOnly]
            public Entity NumberPrefab;
            
            public double ElapsedTime;
            
            public float3 CameraPosition;

            public void Execute()
            {
                
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
                    quaternion vfxRotation = TransformHelpers.LookAtRotation(CameraPosition, numbersPosition, math.up());
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

        [BurstCompile]
        public partial struct UpdateVfxNumbers : IJobEntity
        {
            private const float VFX_LIFETIME = 1.5f;
            private const float VFX_SPEED = 1f;
            
            public double ElapsedTime;
            public float DeltaTime;
            
            public float3 CameraPosition;
            
            public EntityCommandBuffer Ecb;
            
            public void Execute(Entity entity, ref LocalTransform localTransform, in DamageNumberVfx damageNumberVfx, in DynamicBuffer<Child> childrenBuffer)
            {
                if (ElapsedTime - damageNumberVfx.CreationElaspedTime < VFX_LIFETIME)
                {
                    float3 newPosition = localTransform.Position + new float3(0f, VFX_SPEED * DeltaTime, 0f);
                    quaternion newRotation = TransformHelpers.LookAtRotation(CameraPosition, newPosition, math.up());
                    //quaternion newRotation = TransformHelpers.LookAtRotation(newPosition, CameraPosition, -math.up());
                    
                    localTransform.Position = newPosition;
                    localTransform.Rotation = newRotation;

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