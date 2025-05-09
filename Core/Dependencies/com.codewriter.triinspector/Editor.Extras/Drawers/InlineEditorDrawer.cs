﻿using Omni.Inspector.Elements;
using Omni.Inspector.Utilities;
using Omni.Inspector;
using Omni.Inspector.Drawers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: RegisterTriAttributeDrawer(typeof(InlineEditorDrawer), TriDrawerOrder.Decorator,
    ApplyOnArrayElement = true)]

namespace Omni.Inspector.Drawers
{
    public class InlineEditorDrawer : TriAttributeDrawer<InlineEditorAttribute>
    {
        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            if (!typeof(Object).IsAssignableFrom(propertyDefinition.FieldType))
            {
                return "[InlineEditor] valid only on Object fields";
            }

            return TriExtensionInitializationResult.Ok;
        }

        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            var element = new TriBoxGroupElement(new TriBoxGroupElement.Props
            {
                titleMode = TriBoxGroupElement.TitleMode.Hidden,
            });
            element.AddChild(new ObjectReferenceFoldoutDrawerElement(property));
            element.AddChild(new InlineEditorElement(property, new InlineEditorElement.Props
            {
                mode = Attribute.Mode,
                previewHeight = Attribute.PreviewHeight,
            }));
            return element;
        }

        private class ObjectReferenceFoldoutDrawerElement : TriElement
        {
            private readonly TriProperty _property;

            public ObjectReferenceFoldoutDrawerElement(TriProperty property)
            {
                _property = property;
            }

            public override float GetHeight(float width)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position)
            {
                var prefixRect = new Rect(position)
                {
                    height = EditorGUIUtility.singleLineHeight,
                    xMax = position.xMin + EditorGUIUtility.labelWidth,
                };
                var pickerRect = new Rect(position)
                {
                    height = EditorGUIUtility.singleLineHeight,
                    xMin = prefixRect.xMax,
                };

                TriEditorGUI.Foldout(prefixRect, _property);

                EditorGUI.BeginChangeCheck();

                var allowSceneObjects = _property.PropertyTree.TargetIsPersistent == false;

                var value = (Object) _property.Value;
                value = EditorGUI.ObjectField(pickerRect, GUIContent.none, value,
                    _property.FieldType, allowSceneObjects);

                if (EditorGUI.EndChangeCheck())
                {
                    _property.SetValue(value);
                }
            }
        }
    }
}