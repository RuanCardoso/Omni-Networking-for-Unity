﻿using Omni.Inspector.Resolvers;
using Omni.Inspector;
using Omni.Inspector.Processors;

[assembly: RegisterTriPropertyHideProcessor(typeof(HideIfProcessor))]

namespace Omni.Inspector.Processors
{
    public class HideIfProcessor : TriPropertyHideProcessor<HideIfAttribute>
    {
        private ValueResolver<object> _conditionResolver;

        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            base.Initialize(propertyDefinition);

            _conditionResolver = ValueResolver.Resolve<object>(propertyDefinition, Attribute.Condition);

            if (_conditionResolver.TryGetErrorString(out var error))
            {
                return error;
            }

            return TriExtensionInitializationResult.Ok;
        }

        public sealed override bool IsHidden(TriProperty property)
        {
            var val = _conditionResolver.GetValue(property);
            var equal = val?.Equals(Attribute.Value) ?? Attribute.Value == null;
            return equal != Attribute.Inverse;
        }
    }
}