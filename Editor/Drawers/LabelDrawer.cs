#if UNITY_EDITOR
using Omni.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace Omni.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer: PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute labelAttribute = attribute as LabelAttribute;
                           label.text     = labelAttribute.Label;
            EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}
#endif
