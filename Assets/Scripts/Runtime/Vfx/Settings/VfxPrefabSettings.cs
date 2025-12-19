using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.VFX;

namespace Survivor.Runtime.Vfx
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    // TODO: do a specific drawer and enforce its uniqueness.
    /// <summary>
    /// Represents a unique ID for a VFX.
    /// </summary>
    [System.Serializable]
    public struct VfxId : IEquatable<VfxId>
    {
        public ushort Value;

        public bool Equals(VfxId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is VfxId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(VfxId id1, VfxId id2)
        {
            return id1.Equals(id2);
        }

        public static bool operator !=(VfxId id1, VfxId id2)
        {
            return !(id1 == id2);
        }
    }

    /// <summary>
    /// Contains all the settings linked to a gameobject vfx to spawn.
    /// </summary>
    [CreateAssetMenu(fileName = "VfxPrefabSettings", menuName = "Survivor/Vfx/VfxPrefabSettings")]
    public class VfxPrefabSettings : ScriptableObject
    {
        #region Fields
        
        [SerializeField]
        private VfxId _id;
        
        [SerializeField]
        private AssetReference _assetReference;

        [SerializeField]
        private Vector3 _localPosition;
        
        // TODO: if by any chance we need several VisualEffect on the prefab, add container for secondary IVfxSettings.
        /// <summary>
        /// Contains all the specific vfx data to tweak the visual effect in the prefab.
        /// </summary>
        [SerializeField]
        private IVfxSettings _vfxSettings;
        
        #endregion Fields

        #region Properties

        public VfxId ID => _id;
        
        public AssetReference AssetReference => _assetReference;
        
        public Vector3 LocalPosition => _localPosition;
        

        #endregion Properties

        public void InitializeVfx(GameObject vfxInstance, Entity entity, EntityCommandBuffer ecb)
        {
            ecb.SetComponent(entity, new LocalTransform()
            {
                Position = _localPosition,
                Rotation = Quaternion.identity,
                Scale = 1.0f
            });

            var mainVfx = vfxInstance.GetComponent<VisualEffect>();
            _vfxSettings.InitializeVfx(mainVfx);
        }
    }
}