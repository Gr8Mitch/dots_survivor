namespace Survivor.Editor.Core
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    using Survivor.Runtime.Core;

    /// <summary>
    /// An abstract drawer for any custom id.
    /// Should be used for ids implementing <see cref="ICustomId{T}"/>
    /// </summary>
    public abstract class IdDrawer<TId> : PropertyDrawer where TId : unmanaged, ICustomId<TId>
    {
        // This is kind of crappy but I did not find any better way to do that.
        private int _previousValue;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var idProperty = property.FindPropertyRelative(GetIdValuePropertyName());
            var idField = new PropertyField(idProperty, GetIdValueDisplayName());
            container.Add(idField);
            idField.RegisterValueChangeCallback(OnValueChanged);
            idField.RegisterCallback<ChangeEvent<int>>(OnChangeInt);
            
            // A button to generate a new id.
            var generateIdButton = new Button(() => GenerateAvailableId(idProperty));
            generateIdButton.text = "Generate Id";
            container.Add(generateIdButton);
            
            return container;
        }
        
        protected virtual string GetIdValuePropertyName()
        {
            return "Value";
        }
        
        protected virtual string GetIdValueDisplayName()
        {
            return "Id";
        }
        
        private void OnChangeInt(ChangeEvent<int> evt)
        {
            _previousValue = evt.previousValue;
        }
        
        protected virtual void OnValueChanged(SerializedPropertyChangeEvent evt)
        {
            var property = evt.changedProperty;
            int newValue = property.intValue;
            bool isValid = IsValueValid(newValue);

            if (!isValid)
            {
                if (IsValueValid(_previousValue))
                {
                    property.intValue = _previousValue;
                    Debug.LogError($"Id not valid, reverting to {_previousValue}.");
                }
                else
                {
                    GenerateAvailableId(property);
                    Debug.LogWarning($"Id not valid, generating new available id: {property.intValue}.");
                }
                
                return;
            }
            
            property.intValue = newValue;
            property.serializedObject.ApplyModifiedProperties();
        }

        protected abstract bool IsValueValid(int value);

        private void GenerateAvailableId(SerializedProperty idValueProperty)
        {
            idValueProperty.intValue = GenerateAvailableId().ToInt();
            idValueProperty.serializedObject.ApplyModifiedProperties();
        }
        
        protected abstract TId GenerateAvailableId();
        protected abstract TId[] GetUsedIds();
    }
}