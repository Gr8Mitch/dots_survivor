namespace Survivor.Editor.Character
{
    using System;
    using Survivor.Editor.Core;
    using Survivor.Runtime.Character;
    using UnityEditor;
    using UnityEngine;
    
    [CustomPropertyDrawer(typeof(EnemyTypeId))]
    public class EnemyTypeIdDrawer : IdDrawer<EnemyTypeId>
    {
        protected override bool IsValueValid(int value)
        {
            return new EnemyTypeId().SetFromInt(value).IsValid();
        }

        protected override EnemyTypeId GenerateAvailableId()
        {
            var usedIds = GetUsedIds();

            // We start from the min id
            var idCandidate = EnemyTypeId.GetMinId();
            while (idCandidate.IsValid())
            {
                if (!Array.Exists(usedIds, usedId => usedId == idCandidate))
                {
                    return idCandidate;
                }
                
                idCandidate = idCandidate.GetNext();
            }

            if (!idCandidate.IsValid())
            {
                Debug.LogError("Can't find a valid available AbilityId");
            }

            return idCandidate;
        }

        protected override EnemyTypeId[] GetUsedIds()
        {
            return EnemyTypeId.GetUsedIds();
        }
    }
}