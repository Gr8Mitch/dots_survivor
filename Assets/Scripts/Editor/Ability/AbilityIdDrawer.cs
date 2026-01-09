namespace Survivor.Editor.Ability
{
    using Survivor.Editor.Core;
    using Survivor.Runtime.Ability;
    using UnityEditor;
    using System;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(AbilityId))]
    public class AbilityIdDrawer : IdDrawer<AbilityId>
    {
        protected override bool IsValueValid(int value)
        {
            return new AbilityId().SetFromInt(value).IsValid();
        }

        protected override AbilityId GenerateAvailableId()
        {
            var usedIds = GetUsedIds();

            // We start from the min id
            var idCandidate = AbilityId.GetMinId();
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

        protected override AbilityId[] GetUsedIds()
        {
            return AbilityId.GetUsedIds();
        }
    }
}