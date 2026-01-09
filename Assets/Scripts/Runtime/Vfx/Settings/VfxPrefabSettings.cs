namespace Survivor.Runtime.Vfx
{
#if UNITY_EDITOR
    using UnityEditor;
#endif //UNITY_EDITOR
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using System;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine.VFX;
    using Survivor.Runtime.Core;
    
    /// <summary>
    /// Represents a unique ID for a VFX.
    /// </summary>
    [System.Serializable]
    public struct VfxId : IEquatable<VfxId>, IComparable<VfxId>, ICustomId<VfxId>
    {
        public static VfxId Invalid = new VfxId(ushort.MaxValue);
        
        public ushort Value;

        public VfxId(ushort value)
        {
            Value = value;
        }
        
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
        
        public bool IsValid()
        {
            return IsValid(Value);
        }

        public VfxId GetNext()
        {
            if (!IsValid())
            {
                return Invalid;
            }

            if (Value + 1 >= GetMaxId().Value)
            {
                return Invalid;
            }
            
            return new VfxId((ushort)(Value + 1));
        }
        
        public int ToInt()
        {
            return Value;
        }

        public VfxId SetFromInt(int idValue)
        {
            if (IsValid(idValue))
            {
                Value = (ushort)idValue;
            }
            else
            {
                Value = Invalid.Value;
            }

            return this;
        }

        public VfxId SetInvalid()
        {
            Value = Invalid.Value;
            return this;
        }

        public static VfxId GetMinId()
        {
            return new VfxId(0);
        }
        
        public static VfxId GetMaxId()
        {
            return new VfxId(ushort.MaxValue - 1);
        }
        
        private bool IsValid(int idValue)
        {
            return idValue >= GetMinId().Value && idValue <= GetMaxId().Value;
        }

        public int CompareTo(VfxId other)
        {
            return Value.CompareTo(other.Value);
        }
        
        #if UNITY_EDITOR
        public static VfxId[] GetUsedIds()
        {
            // Search through all the VfxPrefabSettings
            var guids = GetIdContainersGuids();
            VfxId[] ids = new VfxId[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                // This is the right asset.
                var settings = AssetDatabase.LoadAssetAtPath<VfxPrefabSettings>(assetPath);
                ids[i] = settings.ID;
            }

            return ids;
        }

        public static string[] GetIdContainersGuids()
        {
            return AssetDatabase.FindAssets($"t:VfxPrefabSettings");
        }
        #endif //UNITY_EDITOR
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