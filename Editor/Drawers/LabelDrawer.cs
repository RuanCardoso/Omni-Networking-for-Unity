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
            LabelAttribute labelAttribute = (LabelAttribute)attribute;
                           label.text     = labelAttribute.Label;

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                var alignment                    = EditorStyles.label.alignment;
                    EditorStyles.label.alignment = TextAnchor.UpperLeft;

                property.boolValue           = EditorGUI.Toggle(position, label, property.boolValue);
                EditorStyles.label.alignment = alignment;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
#endif
