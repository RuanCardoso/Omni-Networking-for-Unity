using Omni.Inspector.Resolvers;
using Omni.Inspector;
using Omni.Inspector.Validators;

[assembly: RegisterTriAttributeValidator(typeof(ValidateInputValidator))]

namespace Omni.Inspector.Validators
{
    public class ValidateInputValidator : TriAttributeValidator<ValidateInputAttribute>
    {
        private ValueResolver<TriValidationResult> _resolver;

        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            base.Initialize(propertyDefinition);

            _resolver = ValueResolver.Resolve<TriValidationResult>(propertyDefinition, Attribute.Method);

            if (_resolver.TryGetErrorString(out var error))
            {
                return error;
            }

            return TriExtensionInitializationResult.Ok;
        }

        public override TriValidationResult Validate(TriProperty property)
        {
            if (_resolver.TryGetErrorString(out var error))
            {
                return TriValidationResult.Error(error);
            }

            return _resolver.GetValue(property);
        }
    }
}