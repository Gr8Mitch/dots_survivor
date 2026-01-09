namespace Survivor.Editor.Vfx
{
    using UnityEngine;
    using UnityEditor;
    using Survivor.Runtime.Vfx;
    using Survivor.Editor.Core;
    using System;
    
    /// <summary>
    /// A custom drawer for the <see cref="VfxId"/>s.
    /// </summary>
    [CustomPropertyDrawer(typeof(VfxId))]
    public class VfxIdDrawer : IdDrawer<VfxId>
    {
        protected override string GetIdValueDisplayName()
        {
            return "Vfx Id";
        }

        protected override bool IsValueValid(int value)
        {
            return new VfxId().SetFromInt(value).IsValid();
        }

        protected override VfxId GenerateAvailableId()
        {
            var usedIds = GetUsedIds();

            // We start from the min id
            var idCandidate = VfxId.GetMinId();
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
                Debug.LogError("Can't find a valid available VfxId");
            }

            return idCandidate;
        }

        protected override VfxId[] GetUsedIds()
        {
            return VfxId.GetUsedIds();
        }
    }
}