namespace Survivor.Editor.Ability
{
    using Survivor.Runtime.Ability;
    using UnityEditor;
    using UnityEngine.UIElements;
    using Survivor.Editor.Core;
    using System.Collections.Generic;
    
    [CustomPropertyDrawer(typeof(AbilityIdReferenceAttribute))]
    public class AbilityIdReferenceDrawer : IdReferenceDrawer
    {
        protected override bool FillDropdownWithIds(DropdownField dropdownField, SerializedProperty idProperty)
        {
            var containersGuids = AbilityId.GetIdContainersGuids();
            int selectedIndex = -1;
            int currentId = idProperty.intValue;
            List<string> idAndNames = new List<string>(containersGuids.Length + 1);
            idAndNames.Add("Invalid");
            
            for (int i = 0; i < containersGuids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(containersGuids[i]);
                // This is the right asset.
                var settings = AssetDatabase.LoadAssetAtPath<IAbilitySettings>(assetPath);
                idAndNames.Add($"{settings.AbilityId.Value} - {settings.DebugName}");

                if (settings.AbilityId.Value == currentId)
                {
                    selectedIndex = i + 1;
                }
            }
            
            dropdownField.choices = idAndNames;

            if (selectedIndex != -1)
            {
                dropdownField.index = selectedIndex;
            }

            return containersGuids.Length != 0;
        }
        
        protected override void OnSelectedIdChanged(ChangeEvent<string> evt)
        {
            var containersGuids = AbilityId.GetIdContainersGuids();
            var dropdownField = (DropdownField)evt.currentTarget;
            SerializedProperty idProperty = (SerializedProperty)dropdownField.userData;
            
            if (containersGuids.Length == 0)
            {
                // This should be "Invalid" in this case.
                idProperty.boxedValue = AbilityId.Invalid;
                idProperty.serializedObject.ApplyModifiedProperties();
                return;
            }
            
            // Extract the correct id from the dropdown choice.
            string selectedChoice = evt.newValue;
            var idString = selectedChoice.Split(" - ")[0];
            if (ushort.TryParse(idString, out ushort id))
            {
                idProperty.boxedValue = new AbilityId(id);
                idProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                // This should be invalid.
                idProperty.boxedValue = AbilityId.Invalid;
                idProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}