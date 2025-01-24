﻿using Omni.Inspector;
using Omni.Inspector.Validators;
using UnityEditor;

[assembly: RegisterTriValueValidator(typeof(TypeMismatchValidator<>))]

namespace Omni.Inspector.Validators
{
    public class TypeMismatchValidator<T> : TriValueValidator<T>
        where T : UnityEngine.Object
    {
        public override TriValidationResult Validate(TriValue<T> propertyValue)
        {
            if (propertyValue.Property.TryGetSerializedProperty(out var serializedProperty) &&
                serializedProperty.propertyType == SerializedPropertyType.ObjectReference &&
                serializedProperty.objectReferenceValue != null &&
                (serializedProperty.objectReferenceValue is T) == false)
            {
                var displayName = propertyValue.Property.DisplayName;
                var actual = serializedProperty.objectReferenceValue.GetType().Name;
                var expected = propertyValue.Property.FieldType.Name;
                var msg = $"{displayName} does not match the type: actual = {actual}, expected = {expected}";
                return TriValidationResult.Warning(msg);
            }

            return TriValidationResult.Valid;
        }
    }
}