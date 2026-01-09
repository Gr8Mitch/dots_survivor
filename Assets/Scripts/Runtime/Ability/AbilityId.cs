namespace Survivor.Runtime.Ability
{
#if UNITY_EDITOR
    using UnityEditor;
#endif //UNITY_EDITOR
    using System;
    using Survivor.Runtime.Core;

    [System.Serializable]
    public struct AbilityId : IEquatable<AbilityId>, IComparable<AbilityId>, ICustomId<AbilityId>
    {
        public static AbilityId Invalid = new AbilityId(ushort.MaxValue);
        
        public ushort Value;
        
        public AbilityId(ushort value)
        {
            Value = value;
        }

        public bool Equals(AbilityId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is AbilityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(AbilityId id1, AbilityId id2)
        {
            return id1.Equals(id2);
        }

        public static bool operator !=(AbilityId id1, AbilityId id2)
        {
            return !(id1 == id2);
        }
        
        public bool IsValid()
        {
            return IsValid(Value);
        }

        public AbilityId GetNext()
        {
            if (!IsValid())
            {
                return Invalid;
            }

            if (Value + 1 >= GetMaxId().Value)
            {
                return Invalid;
            }
            
            return new AbilityId((ushort)(Value + 1));
        }
        
        public int ToInt()
        {
            return Value;
        }

        public AbilityId SetFromInt(int idValue)
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

        public AbilityId SetInvalid()
        {
            Value = Invalid.Value;
            return this;
        }

        public static AbilityId GetMinId()
        {
            return new AbilityId(0);
        }
        
        public static AbilityId GetMaxId()
        {
            return new AbilityId(ushort.MaxValue - 1);
        }
        
        private bool IsValid(int idValue)
        {
            return idValue >= GetMinId().Value && idValue <= GetMaxId().Value;
        }

        public int CompareTo(AbilityId other)
        {
            return Value.CompareTo(other.Value);
        }
        
        #if UNITY_EDITOR
        public static AbilityId[] GetUsedIds()
        {
            // Search through all the VfxPrefabSettings
            var guids = GetIdContainersGuids();
            AbilityId[] ids = new AbilityId[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                // This is the right asset.
                var settings = AssetDatabase.LoadAssetAtPath<IAbilitySettings>(assetPath);
                ids[i] = settings.AbilityId;
            }

            return ids;
        }

        public static string[] GetIdContainersGuids()
        {
            return AssetDatabase.FindAssets($"t:IAbilitySettings");
        }
        #endif //UNITY_EDITOR
    }
}