using Omni.Inspector;
using Omni.Inspector.Validators;
using UnityEditor;

[assembly: RegisterTriValueValidator(typeof(MissingReferenceValidator))]

namespace Omni.Inspector.Validators
{
    public class MissingReferenceValidator : TriValueValidator<UnityEngine.Object>
    {
        public override TriValidationResult Validate(TriValue<UnityEngine.Object> propertyValue)
        {
            if (propertyValue.Property.TryGetSerializedProperty(out var serializedProperty) &&
                serializedProperty.propertyType == SerializedPropertyType.ObjectReference &&
                serializedProperty.objectReferenceValue == null &&
                serializedProperty.objectReferenceInstanceIDValue != 0)
            {
                return TriValidationResult.Warning($"{GetName(propertyValue.Property)} is missing");
            }

            return TriValidationResult.Valid;
        }

        private static string GetName(TriProperty property)
        {
            var name = property.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                name = property.RawName;
            }

            return name;
        }
    }
}