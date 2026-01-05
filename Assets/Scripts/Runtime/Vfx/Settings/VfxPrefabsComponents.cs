namespace Survivor.Runtime.Vfx
{
    using Unity.Entities;

    /// <summary>
    /// Contains the id of the VFX prefab linked to some entity.
    /// The vfx entity should be a child of the entity with this component.
    /// </summary>
    public struct VfxPrefabId : IComponentData
    {
        public VfxId Value;
    }
    
    /// <summary>
    /// A tag component to indicate that the VFX prefab has not been created yet.
    /// </summary>
    public struct VfxPrefabNotCreated : IComponentData { }

    /// <summary>
    /// A component on a entity to indicate that a VFX prefab has started loading.
    /// IT is a <see cref="ICleanupComponentData"/> to be able to detect vfx that were loading but the related entity
    /// was destroyed before the loading was completed.
    /// It is supposed to be added to a specific entity, not the one with the VfxPrefabId
    /// </summary>
    public struct VfxPrefabLoading : ICleanupComponentData
    {
        public Entity Owner;
        /// <summary>
        /// It is an unique Id to know which async operation handle is linked to this component.
        /// </summary>
        public int LoadingId;
        public VfxId VfxId;
    }
    
    /// <summary>
    /// A tag component to indicate to detect if an entity with VfxPrefabLoading actually needs to be cleaned.
    /// I didn't find a better way to do it because basically the entity only needs <see cref="VfxPrefabLoading"/>
    /// which is a <see cref="ICleanupComponentData"/>
    /// </summary>
    public struct VfxPrefabLoadingIsValid : IComponentData { }
}