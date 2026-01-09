namespace Survivor.Editor.Core
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary>
    /// An abstract drawer for reference to any custom id.
    /// </summary>
    public abstract class IdReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            
            SerializedProperty idProperty = property.FindPropertyRelative(GetIdValuePropertyName());
            var dropdownField = new DropdownField(property.name);
            dropdownField.userData = property;
            FillDropdownWithIds(dropdownField, idProperty);

            dropdownField.RegisterValueChangedCallback(OnSelectedIdChanged);
            container.Add(dropdownField);
            
            return container;
        }
        
        protected abstract void OnSelectedIdChanged(ChangeEvent<string> evt);

        protected abstract bool FillDropdownWithIds(DropdownField dropdownField, SerializedProperty idProperty);
        

        protected virtual string GetIdValuePropertyName()
        {
            return "Value";
        }
        
        protected virtual string GetIdValueDisplayName()
        {
            return "Id";
        }
    }
}