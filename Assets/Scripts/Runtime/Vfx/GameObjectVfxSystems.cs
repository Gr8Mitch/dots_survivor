namespace Survivor.Runtime.Vfx
{
    using UnityEngine;
    using Unity.Entities;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Transforms;
    using UnityEngine.AddressableAssets;
    using Unity.Burst;
    using UnityEngine.Jobs;
    using Survivor.Runtime.Character;
    
    // TODO_IMPROVEMENT : Handle the destruction of the vfx in case the entity is destroyed.
    public struct VfxEntityProxy : ICleanupComponentData
    {
        /// <summary>
        /// In case of vfx linked to abilities, this is the ability entity.
        /// </summary>
        public Entity Owner;

        public UnityObjectRef<GameObject> VfxGameObject;
    }

    /// <summary>
    /// Component placed on the owner of the entity to detect when the owner is destroyed so that the
    /// corresponding vfx is also destroyed. 
    /// </summary>
    public struct VfxEntityProxyReference : ICleanupComponentData
    {
        public Entity ProxyEntity;
    }

    // TODO_IMPROVEMENT: It will also in charge of destroying the gameobject vfx when the entity is destroyed.
    /// <summary>
    /// Instantiate a gameobject vfx and make the link between the entity proxy and the gameobject vfx.
    /// Even if the vfx is already loaded, there will be a one frame delay before the vfx is actually instantiated.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class InstantiateGameObjectVfxSystem : SystemBase
    {
        // TODO_IMPROVEMENT: use pools in case of short lived Vfxs.
        private AsyncOperationHandle<VfxPrefabsSettingsContainer> _vfxPrefabsSettingsContainerHandle;
        private VfxPrefabsSettingsContainer _vfxPrefabsSettingsContainer;

        private Dictionary<int, AsyncOperationHandle<GameObject>> _loadingVfxHandleByLoadingId = new();
        private Dictionary<Entity, AsyncOperationHandle<GameObject>> _vfxHandleByEntityProxy = new();
        private Dictionary<Entity, GameObject> _vfxGameObjectsByEntityProxy = new();

        private EntityArchetype _loadingVfxEntityArchetype;
        private EntityArchetype _vfxProxyEntityArchetype;
        private EntityQuery _vfxPrefabsToLoadQuery;
        private EntityQuery _loadingVfxPrefabsQuery;
        private int _nextVfxPrefabLoadId = 0;

        private bool _hasVfxGameObjectsChanged = false;
        
        public Dictionary<Entity, GameObject> VfxGameObjectsByEntityProxy => _vfxGameObjectsByEntityProxy;
        public bool HasVfxGameObjectsChangedSinceLastFrame
        {
            get
            {
                return _hasVfxGameObjectsChanged;
            }
            set
            {
                _hasVfxGameObjectsChanged = value;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _vfxPrefabsSettingsContainerHandle = VfxPrefabsSettingsContainer.LoadAsync();
            // We force the load to be synchronous to simply things (at least for now).
            _vfxPrefabsSettingsContainerHandle.WaitForCompletion();
            _vfxPrefabsSettingsContainer = _vfxPrefabsSettingsContainerHandle.Result;

            if (_vfxPrefabsSettingsContainer == null)
            {
                Debug.LogError("Failed to load VfxPrefabsSettingsContainer");
            }

            _vfxPrefabsToLoadQuery = SystemAPI.QueryBuilder()
                .WithAll<VfxPrefabNotCreated, VfxPrefabId>()
                .Build();

            _loadingVfxEntityArchetype =
                EntityManager.CreateArchetype(typeof(VfxPrefabLoading), typeof(VfxPrefabLoadingIsValid));
            _loadingVfxPrefabsQuery = SystemAPI.QueryBuilder()
                .WithAll<VfxPrefabLoading, VfxPrefabLoadingIsValid>()
                .Build();

            _vfxProxyEntityArchetype = EntityManager.CreateArchetype(typeof(VfxEntityProxy),
                typeof(LocalToWorld), typeof(LocalTransform));

            RequireAnyForUpdate(_vfxPrefabsToLoadQuery, _loadingVfxPrefabsQuery);
            RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        protected override void OnDestroy()
        {
            if (_vfxPrefabsSettingsContainerHandle.IsValid())
            {
                _vfxPrefabsSettingsContainerHandle.Release();
            }

            foreach (var entry in _loadingVfxHandleByLoadingId)
            {
                entry.Value.Release();
            }

            _loadingVfxHandleByLoadingId.Clear();
            _loadingVfxHandleByLoadingId = null;

            foreach (var entry in _vfxHandleByEntityProxy)
            {
                entry.Value.Release();
            }

            _vfxHandleByEntityProxy.Clear();
            _vfxHandleByEntityProxy = null;
            
            _vfxGameObjectsByEntityProxy.Clear();
            _vfxGameObjectsByEntityProxy = null;

            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            _hasVfxGameObjectsChanged = false;

            // Detect vfx that we should start to load.
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            foreach (var (vfxPrefabId, entity) in SystemAPI.Query<RefRO<VfxPrefabId>>()
                         .WithAll<VfxPrefabNotCreated>()
                         .WithEntityAccess())
            {
                var vfxPrefabSettings = _vfxPrefabsSettingsContainer.GetPrefabSettings(vfxPrefabId.ValueRO.Value);
                if (vfxPrefabSettings == null)
                {
                    Debug.LogError($"Failed to find VfxPrefabSettings for VfxPrefabId {vfxPrefabId.ValueRO.Value}");
                    continue;
                }

                var handle = Addressables.LoadAssetAsync<GameObject>(vfxPrefabSettings.AssetReference.RuntimeKey);
                _loadingVfxHandleByLoadingId.Add(_nextVfxPrefabLoadId, handle);
                var loadingVfxEntity = ecb.CreateEntity(_loadingVfxEntityArchetype);
                ecb.SetComponent(loadingVfxEntity, new VfxPrefabLoading()
                {
                    Owner = entity,
                    VfxId = vfxPrefabId.ValueRO.Value,
                    LoadingId = _nextVfxPrefabLoadId++
                });
            }

            ecb.RemoveComponent(_vfxPrefabsToLoadQuery, ComponentType.ReadOnly<VfxPrefabNotCreated>(),
                EntityQueryCaptureMode.AtPlayback);

            if (!_loadingVfxPrefabsQuery.IsEmpty)
            {
                var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
                var characterBodyLookup = SystemAPI.GetComponentLookup<CharacterBodyData>(true);
                var characterAbilityVfxOwnerComponentLookup = SystemAPI.GetComponentLookup<CharacterAbilityVfxOwnerComponent>(true);
                
                // We can't use SystemAPI.Query here as we need to use the Entity Manager.
                var vfxPrefabLoadingArray =
                    _loadingVfxPrefabsQuery.ToComponentDataArray<VfxPrefabLoading>(Allocator.Temp);
                var entitiesArray = _loadingVfxPrefabsQuery.ToEntityArray(Allocator.Temp);

                for (int i = 0; i < vfxPrefabLoadingArray.Length; ++i)
                {
                    var vfxPrefabLoading = vfxPrefabLoadingArray[i];
                    var entity = entitiesArray[i];
                    if (_loadingVfxHandleByLoadingId.TryGetValue(vfxPrefabLoading.LoadingId, out var handle))
                    {
                        if (handle.IsDone)
                        {
                            if (handle.Status == AsyncOperationStatus.Succeeded)
                            {
                                // Instantiate the vfx prefab and create the vfx entity proxy.
                                GameObject vfxPrefab = handle.Result;
                                var vfxInstance = Object.Instantiate(vfxPrefab);
                                var ownerEntity = vfxPrefabLoading.Owner;
                                if (ownerEntity != Entity.Null)
                                {
                                    var settings =
                                        _vfxPrefabsSettingsContainer.GetPrefabSettings(vfxPrefabLoading.VfxId);
                                    // We need the real Entity right now, so we can't use the EntityCommandBuffer in this case.
                                    var proxyEntity = EntityManager.CreateEntity(_vfxProxyEntityArchetype);

                                    if (characterAbilityVfxOwnerComponentLookup.TryGetComponent(ownerEntity,
                                            out var abilityVfxOwner))
                                    {
                                        // In this case, the vfx has not parent, a dedicated system updates the position/rotation
                                        // We need to find the character parent.
                                        Entity parentEntity = ownerEntity;
                                        while (!characterBodyLookup.HasComponent(parentEntity))
                                        {
                                            if (parentLookup.TryGetComponent(parentEntity, out var parent))
                                            {
                                                parentEntity = parent.Value;
                                            }
                                            else
                                            {
                                                // This is the root entity and we did not find the character, something is wrong.
                                                Debug.LogError($"No character found on the root of the vfx owner {vfxPrefabLoading.VfxId}");
                                                parentEntity = Entity.Null;
                                                break;
                                            }
                                        }

                                        if (parentEntity != Entity.Null)
                                        {
                                            ecb.AddComponent(proxyEntity, new CharacterAbilityVfxComponent()
                                            {
                                                CharacterEntity = parentEntity,
                                                PositionOffset = settings.LocalPosition,
                                                ReplicateCharacterPosition = abilityVfxOwner.ReplicateCharacterPosition,
                                                AlignWithCharacterGround = abilityVfxOwner.AlignWithCharacterGround
                                            });
                                        }
                                    }
                                    else
                                    {
                                        ecb.AddComponent(proxyEntity, new Parent()
                                        {
                                            Value = ownerEntity
                                        });
                                    }
                                    
                                    ecb.SetComponent(proxyEntity, new VfxEntityProxy()
                                    {
                                        Owner = ownerEntity,
                                        VfxGameObject = vfxInstance
                                    });
                                    
                                    ecb.AddComponent(ownerEntity, new VfxEntityProxyReference()
                                    {
                                        ProxyEntity = proxyEntity
                                    });

                                    settings.InitializeVfx(vfxInstance, proxyEntity, ecb);

                                    _vfxHandleByEntityProxy.Add(proxyEntity, handle);
                                    _vfxGameObjectsByEntityProxy.Add(proxyEntity, vfxInstance);
                                    _hasVfxGameObjectsChanged = true;
                                }
                                else
                                {
                                    // TODO_FEATURE: handle vfx without owner, if we ever have this case.
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to load vfx prefab {vfxPrefabLoading.VfxId}");
                            }
                        }
                        else
                        {
                            // Wait for the next frames.
                            continue;
                        }
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to find loading handle for VfxPrefabLoading {vfxPrefabLoading.LoadingId}");
                    }

                    _loadingVfxHandleByLoadingId.Remove(vfxPrefabLoading.LoadingId);
                    ecb.RemoveComponent<VfxPrefabLoading>(entity);
                    ecb.DestroyEntity(entity);
                }

                // Clean up loading vfx prefabs that were never loaded. But is this really useful?
                foreach (var (vfxPrefabLoading, entity) in SystemAPI.Query<RefRO<VfxPrefabLoading>>()
                             .WithNone<VfxPrefabLoadingIsValid>().WithEntityAccess())
                {
                    // The entity was destroyed before the loading was completed. Clear everything.
                    if (_loadingVfxHandleByLoadingId != null &&
                        _loadingVfxHandleByLoadingId.TryGetValue(vfxPrefabLoading.ValueRO.LoadingId, out var handle))
                    {
                        handle.Release();
                        _loadingVfxHandleByLoadingId.Remove(vfxPrefabLoading.ValueRO.LoadingId);
                    }

                    ecb.RemoveComponent<VfxPrefabLoading>(entity);
                    ecb.DestroyEntity(entity);
                }
            }
            
            // Clean up vfx gameobjects when the entity proxy is destroyed.
            foreach (var (vfxEntityProxy, entity) in SystemAPI.Query<RefRO<VfxEntityProxy>>()
                         .WithNone<LocalTransform>().WithEntityAccess())
            {
                // The entity was destroyed. Clear everything and destroy the linked gameobject.
                if (_vfxHandleByEntityProxy != null &&
                    _vfxHandleByEntityProxy.TryGetValue(entity, out var handle))
                {
                    handle.Release();
                    _vfxHandleByEntityProxy.Remove(entity);
                    _hasVfxGameObjectsChanged = true;
                }

                ecb.RemoveComponent<VfxEntityProxy>(entity);
                ecb.DestroyEntity(entity);

                if (vfxEntityProxy.ValueRO.VfxGameObject.IsValid())
                {
                    Object.Destroy(vfxEntityProxy.ValueRO.VfxGameObject);
                }
            }

            var vfxEntityProxyLookup = SystemAPI.GetComponentLookup<VfxEntityProxy>(true);
            // Clean up vfx gameobjects when the entity proxy's owner is destroyed.
            foreach (var (vfxEntityProxyReference, entity) in SystemAPI.Query<RefRO<VfxEntityProxyReference>>()
                         .WithNone<LocalTransform>().WithEntityAccess())
            {
                // The entity was destroyed. Clear everything and destroy the linked gameobject.
                var proxyEntity = vfxEntityProxyReference.ValueRO.ProxyEntity;
                if (_vfxHandleByEntityProxy != null &&
                    _vfxHandleByEntityProxy.TryGetValue(proxyEntity, out var handle))
                {
                    handle.Release();
                    _vfxHandleByEntityProxy.Remove(proxyEntity);
                    _hasVfxGameObjectsChanged = true;
                }

                ecb.RemoveComponent<VfxEntityProxy>(entity);
                ecb.DestroyEntity(entity);

                if (vfxEntityProxyLookup.TryGetComponent(proxyEntity, out var vfxEntityProxy) && vfxEntityProxy.VfxGameObject.IsValid())
                {
                    Object.Destroy(vfxEntityProxy.VfxGameObject);
                }
            }
        }
    }

    /// <summary>
    /// Synchronizes the position of the vfx gameobject from the entity proxy.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class SynchronizeVfxGameObjectFromProxy : SystemBase
    {
        // TODO_IMPROVEMENT: should move some data in a singleton or something equivalent to remove this hard dependency?
        /// <summary>
        /// This is the system that contains the data linkind the entity proxy and the gameobject.
        /// </summary>
        private InstantiateGameObjectVfxSystem _instantiateGameObjectVfxSystem;
        
        /// <summary>
        /// The transform access array used to sync the gameobject transform from the one from the entity proxy.
        /// </summary>
        private TransformAccessArray _transformAccessArray;

        private EntityQuery _vfxProxyQuery;
        private NativeList<Entity> _entitiesToUpdate;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            _vfxProxyQuery = SystemAPI.QueryBuilder().WithAll<VfxEntityProxy, LocalToWorld>().Build();
            RequireForUpdate(_vfxProxyQuery);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (_instantiateGameObjectVfxSystem == null)
            {
                _instantiateGameObjectVfxSystem = World.GetExistingSystemManaged<InstantiateGameObjectVfxSystem>();
                InitializeTransformContainers(_vfxProxyQuery.CalculateEntityCount());
            }
        }

        protected override void OnDestroy()
        {
            _entitiesToUpdate.Dispose();
            _transformAccessArray.Dispose();
            
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (_instantiateGameObjectVfxSystem.HasVfxGameObjectsChangedSinceLastFrame)
            {
                InitializeTransformContainers(_vfxProxyQuery.CalculateEntityCount());
                // TODO_IMPROVEMENT: improve this, this is ugly.
                // We need to update the value as the system might not update next frame.
                _instantiateGameObjectVfxSystem.HasVfxGameObjectsChangedSinceLastFrame = false;
            }

            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var job = new SynchronizeVfxEntityTransform()
            {
                EntitiesToUpdate = _entitiesToUpdate,
                LocalToWorldLookup = localToWorldLookup
            };
            
            Dependency = job.Schedule(_transformAccessArray, Dependency);
        }

        private void InitializeTransformContainers(int vfxCount)
        {
            // I did not find any way to just clear the TransformAccessArray, so I just recreate it. 
            int capacity = Mathf.Max(16, Mathf.NextPowerOfTwo(vfxCount));
            if (_transformAccessArray.isCreated)
            {
                _transformAccessArray.Dispose();
            }
            _transformAccessArray = new TransformAccessArray(capacity);
            
            if (!_entitiesToUpdate.IsCreated)
            {
                _entitiesToUpdate = new NativeList<Entity>(capacity, Allocator.Persistent);
            }
            else
            {
                _entitiesToUpdate.ResizeUninitialized(capacity);
                _entitiesToUpdate.Clear();
            }
            
            var gameObjectsByEntityProxy = _instantiateGameObjectVfxSystem.VfxGameObjectsByEntityProxy;
            foreach (var entry in gameObjectsByEntityProxy)
            {
                _entitiesToUpdate.Add(entry.Key);
                _transformAccessArray.Add(entry.Value.transform);
            }
        }
        
        [BurstCompile]
        private struct SynchronizeVfxEntityTransform : IJobParallelForTransform
        {
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            [ReadOnly]
            public NativeList<Entity> EntitiesToUpdate;
            
            public void Execute(int index, TransformAccess transform)
            {
                LocalToWorld localToWorld = LocalToWorldLookup[EntitiesToUpdate[index]];
                transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
            }
        }
    }
}