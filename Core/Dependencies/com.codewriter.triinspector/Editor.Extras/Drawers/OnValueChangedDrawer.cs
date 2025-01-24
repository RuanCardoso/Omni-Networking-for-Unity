﻿using Omni.Inspector.Resolvers;
using Omni.Inspector;
using Omni.Inspector.Drawers;

[assembly: RegisterTriAttributeDrawer(typeof(OnValueChangedDrawer), TriDrawerOrder.System)]

namespace Omni.Inspector.Drawers
{
    public class OnValueChangedDrawer : TriAttributeDrawer<OnValueChangedAttribute>
    {
        private ActionResolver _actionResolver;

        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            base.Initialize(propertyDefinition);

            _actionResolver = ActionResolver.Resolve(propertyDefinition, Attribute.Method);
            if (_actionResolver.TryGetErrorString(out var error))
            {
                return error;
            }

            return TriExtensionInitializationResult.Ok;
        }

        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            return new OnValueChangedListenerElement(property, next, _actionResolver);
        }

        private class OnValueChangedListenerElement : TriElement
        {
            private readonly TriProperty _property;
            private readonly ActionResolver _actionResolver;

            public OnValueChangedListenerElement(TriProperty property, TriElement next, ActionResolver actionResolver)
            {
                _property = property;
                _actionResolver = actionResolver;

                AddChild(next);
            }

            protected override void OnAttachToPanel()
            {
                base.OnAttachToPanel();

                _property.ValueChanged += OnValueChanged;
                _property.ChildValueChanged += OnValueChanged;
            }

            protected override void OnDetachFromPanel()
            {
                _property.ChildValueChanged -= OnValueChanged;
                _property.ValueChanged -= OnValueChanged;

                base.OnDetachFromPanel();
            }

            private void OnValueChanged(TriProperty obj)
            {
                _property.PropertyTree.ApplyChanges();
                _actionResolver.InvokeForAllTargets(_property);
                _property.PropertyTree.Update();
            }
        }
    }
}