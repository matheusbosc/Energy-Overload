using UnityEditor;
using UnityEngine;

namespace Sisus.ComponentNames.Editor
{
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.Editor.DrawerPriority(0, 0, 0.26)] // make Odin prioritize other custom property drawers over this one 
#endif
    [CustomPropertyDrawer(typeof(Component), true)]
    [CustomPropertyDrawer(typeof(Object), false)] // needed for UnityEvent property drawer support
    public class RenameableObjectPropertyDrawer : PropertyDrawer
    {
        private static Color ObjectFieldBackgroundColor => EditorGUIUtility.isProSkin ? new Color32(42, 42, 42, 255) : new Color32(237, 237, 237, 255);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Component objectFieldValue = property.objectReferenceValue as Component;
            if(!objectFieldValue || property.serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultObjectField(position, property, label);
                return;
            }
            
            var componentWithField = property.serializedObject.targetObject as Component;
            string textInsideField = GetTextInsideField(objectFieldValue, componentWithField);

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            DrawDefaultObjectField(fieldRect, property, GUIContent.none);
            fieldRect.x += 16f;
            fieldRect.width -= 35f;
            fieldRect.y += 2f;
            fieldRect.height -= 3f;
            EditorGUI.DrawRect(fieldRect, ObjectFieldBackgroundColor);
            GUI.Label(fieldRect, textInsideField);
        }

        private static string GetTextInsideField(Component objectFieldValue, Component componentWithField)
        {
            if(ReferenceEquals(objectFieldValue, componentWithField))
            {
                return "This Component";
            }

            string componentName = ComponentName.Get(objectFieldValue);

            if(componentWithField && objectFieldValue.gameObject == componentWithField.gameObject)
            {
                return string.Concat(componentName, " (this GameObject)");
            }

            string gameObjectName = objectFieldValue.gameObject.name;
            return string.Concat(componentName, " (", gameObjectName, ")");
        }

        protected virtual void DrawDefaultObjectField(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.ObjectField(position, property, label);
        }
    }
}