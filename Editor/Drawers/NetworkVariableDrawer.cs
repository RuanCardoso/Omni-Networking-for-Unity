/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_EDITOR
using Omni.Core;
using Omni.Shared;
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using BindingFlags = System.Reflection.BindingFlags;

namespace Omni.Editor
{
    [CustomPropertyDrawer(typeof(NetworkVariableAttribute), true)]
    [CanEditMultipleObjects]
    public class NetworkVariableDrawer : PropertyDrawer
    {
        private Texture2D quadTexture;
        private PropertyInfo propertyInfo;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (NetworkVariableAttribute)attribute;
            if (attr.TrackChangesInInspector == false)
            {
                label.text = $" {property.displayName}";
                label.image = GetTexture(new Color(1, 0.5f, 0, 1f));
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (
                targetObject
                is NetworkBehaviour
                    or DualBehaviour
                    or ClientBehaviour
                    or ServerBehaviour
            )
            {
                // Validate naming convetion
                string fieldName = fieldInfo.Name;
                if (fieldName.Contains("M_") || char.IsUpper(fieldName[0]))
                {
                    NetworkLogger.__Log__(
                        "NetworkVariable fields must always begin with the first lowercase letter.",
                        NetworkLogger.LogType.Error
                    );

                    return;
                }

                // Find the property
                Type type = targetObject.GetType();
                string propertyName = fieldName.Replace("m_", "");
                propertyName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
                propertyInfo ??= type.GetProperty(
                    propertyName,
                    BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public
                        // | BindingFlags.DeclaredOnly // Find property in base classes
                ); // ??= Optimization.

                if (propertyInfo != null)
                {
                    label.text = $" {property.displayName}";
                    label.image = GetTexture(Color.green);
                    // Update the property!
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, property, label, true);
                    if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                    {
                        try
                        {
                            if (propertyInfo.PropertyType == property.boxedValue.GetType())
                            {
                                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                                propertyInfo.SetValue(targetObject, property.boxedValue);
                            }
                            else
                            {
                                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                                propertyInfo.SetValue(
                                    targetObject,
                                    fieldInfo.GetValue(targetObject)
                                );
                            }
                        }
                        catch
                        {
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            propertyInfo.SetValue(targetObject, fieldInfo.GetValue(targetObject));
                        }
                    }

                    if (UpdateWhenLengthChanges(targetObject) && Application.isPlaying)
                    {
                        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        propertyInfo.SetValue(targetObject, fieldInfo.GetValue(targetObject));
                    }
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"Error: The NetworkVariable requires a property named '{propertyName}' in the class '{type}'.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
            else
            {
                NetworkLogger.__Log__(
                    "Error: The NetworkVariable requires the class to inherit from 'EventBehaviour'.",
                    NetworkLogger.LogType.Error
                );
            }

            EditorGUI.EndProperty();
        }

        private int lastCount = 0;

        private bool UpdateWhenLengthChanges(UnityEngine.Object targetObject)
        {
            if (fieldInfo.GetValue(targetObject) is IEnumerable enumerator)
            {
                int count = 0;
                foreach (var item in enumerator)
                    count++;

                if (count != lastCount)
                {
                    lastCount = count;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        private Texture2D GetTexture(Color color)
        {
            if (quadTexture == null)
            {
                Texture2D whiteTxt = Texture2D.whiteTexture;
                quadTexture = new(whiteTxt.width, whiteTxt.height);

                #region Set Color

                for (int y = 0; y < quadTexture.height; y++)
                {
                    for (int x = 0; x < quadTexture.width; x++)
                    {
                        quadTexture.SetPixel(x, y, color);
                    }
                }

                #endregion

                quadTexture.Apply();
            }

            return quadTexture;
        }
    }
}
#endif
