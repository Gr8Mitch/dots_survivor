namespace Survivor.Runtime.Character
{
    using System;
    using Survivor.Runtime.Core;
    using UnityEditor;
    using UnityEngine;

    
    /// <summary>
    /// Represents a unique ID for a VFX.
    /// </summary>
    [System.Serializable]
    public struct EnemyTypeId : IEquatable<EnemyTypeId>, IComparable<EnemyTypeId>, ICustomId<EnemyTypeId>
    {
        public static EnemyTypeId Invalid = new EnemyTypeId(ushort.MaxValue);
        
        public ushort Value;

        public EnemyTypeId(ushort value)
        {
            Value = value;
        }
        
        public bool Equals(EnemyTypeId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EnemyTypeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(EnemyTypeId id1, EnemyTypeId id2)
        {
            return id1.Equals(id2);
        }

        public static bool operator !=(EnemyTypeId id1, EnemyTypeId id2)
        {
            return !(id1 == id2);
        }
        
        public bool IsValid()
        {
            return IsValid(Value);
        }

        public EnemyTypeId GetNext()
        {
            if (!IsValid())
            {
                return Invalid;
            }

            if (Value + 1 >= GetMaxId().Value)
            {
                return Invalid;
            }
            
            return new EnemyTypeId((ushort)(Value + 1));
        }
        
        public int ToInt()
        {
            return Value;
        }

        public EnemyTypeId SetFromInt(int idValue)
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

        public EnemyTypeId SetInvalid()
        {
            Value = Invalid.Value;
            return this;
        }

        public static EnemyTypeId GetMinId()
        {
            return new EnemyTypeId(0);
        }
        
        public static EnemyTypeId GetMaxId()
        {
            return new EnemyTypeId(ushort.MaxValue - 1);
        }
        
        private bool IsValid(int idValue)
        {
            return idValue >= GetMinId().Value && idValue <= GetMaxId().Value;
        }

        public int CompareTo(EnemyTypeId other)
        {
            return Value.CompareTo(other.Value);
        }
        
        #if UNITY_EDITOR
        public static EnemyTypeId[] GetUsedIds()
        {
            // Search through all the VfxPrefabSettings
            var guids = GetIdContainersGuids();
            EnemyTypeId[] ids = new EnemyTypeId[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                // This is the right asset.
                var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                ids[i] = enemyPrefab.GetComponent<EnemyCharacterComponentAuthoring>().EnemyTypeId;
            }
            
            return ids;
        }

        public static string[] GetIdContainersGuids()
        {
            return AssetDatabase.FindAssets($"t:prefab Enemy_");
        }
        #endif //UNITY_EDITOR
    }
}