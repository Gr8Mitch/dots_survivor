namespace Survivor.Editor.Character
{
    using Survivor.Editor.Core;
    using System.Collections.Generic;
    using Survivor.Runtime.Character;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(EnemyTypeIdReferenceAttribute))]
    public class EnemyTypeIdReferenceDrawer : IdReferenceDrawer
    {
        protected override bool FillDropdownWithIds(DropdownField dropdownField, SerializedProperty idProperty)
        {
            var containersGuids = EnemyTypeId.GetIdContainersGuids();
            int selectedIndex = -1;
            int currentId = idProperty.intValue;
            List<string> idAndNames = new List<string>(containersGuids.Length + 1);
            idAndNames.Add("Invalid");
            
            for (int i = 0; i < containersGuids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(containersGuids[i]);
                // This is the right asset.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                var authoring = prefab.GetComponent<EnemyCharacterComponentAuthoring>();
                idAndNames.Add($"{authoring.EnemyTypeId.Value} - {prefab.name}");

                if (authoring.EnemyTypeId.Value == currentId)
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
            var containersGuids = EnemyTypeId.GetIdContainersGuids();
            var dropdownField = (DropdownField)evt.currentTarget;
            SerializedProperty idProperty = (SerializedProperty)dropdownField.userData;
            
            if (containersGuids.Length == 0)
            {
                // This should be "Invalid" in this case.
                idProperty.boxedValue = EnemyTypeId.Invalid;
                idProperty.serializedObject.ApplyModifiedProperties();
                return;
            }
            
            // Extract the correct id from the dropdown choice.
            string selectedChoice = evt.newValue;
            var idString = selectedChoice.Split(" - ")[0];
            if (ushort.TryParse(idString, out ushort id))
            {
                idProperty.boxedValue = new EnemyTypeId(id);
                idProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                // This should be invalid.
                idProperty.boxedValue = EnemyTypeId.Invalid;
                idProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}