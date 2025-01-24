using Omni.Inspector.Elements;
using Omni.Inspector.Resolvers;
using Omni.Inspector;
using Omni.Inspector.Drawers;

[assembly: RegisterTriAttributeDrawer(typeof(DropdownDrawer<>), TriDrawerOrder.Decorator)]

namespace Omni.Inspector.Drawers
{
    public class DropdownDrawer<T> : TriAttributeDrawer<DropdownAttribute>
    {
        private DropdownValuesResolver<T> _valuesResolver;

        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            _valuesResolver = DropdownValuesResolver<T>.Resolve(propertyDefinition, Attribute.Values);

            if (_valuesResolver.TryGetErrorString(out var error))
            {
                return error;
            }

            return TriExtensionInitializationResult.Ok;
        }

        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            return new TriDropdownElement(property, _valuesResolver.GetDropdownItems);
        }
    }
}