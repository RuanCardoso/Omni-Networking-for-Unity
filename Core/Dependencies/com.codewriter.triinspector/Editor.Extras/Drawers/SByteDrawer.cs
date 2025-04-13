#if DISABLED
using Omni.Inspector;
using Omni.Inspector.Drawers;
using Omni.Inspector.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

[assembly: RegisterTriTypeProcessor(typeof(TriSByteTypeProcessor), 2)]
[assembly: RegisterTriValueDrawer(typeof(ByteDrawer), TriDrawerOrder.Fallback)]

namespace Omni.Inspector.Drawers
{
    public class TriSByteTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            const int propertiesOffset = 6001;

            properties.AddRange(TriReflectionUtilities
                .GetAllInstancePropertiesInDeclarationOrder(type)
                .Where(IsSByteProperty)
                .Select((it, ind) => TriPropertyDefinition.CreateForPropertyInfo(ind + propertiesOffset, it)));
        }

        private static bool IsSByteProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<ShowInInspectorAttribute>(false) != null &&
                   propertyInfo.PropertyType == typeof(sbyte) &&
                   propertyInfo.CanRead &&
                   propertyInfo.CanWrite;
        }
    }

    public class SByteDrawer : BuiltinDrawerBase<sbyte>
    {
        protected override sbyte OnValueGUI(Rect position, GUIContent label, sbyte value)
        {
            var intValue = EditorGUI.IntField(position, label, value);
            return (sbyte) Mathf.Clamp(intValue, sbyte.MinValue, sbyte.MaxValue);
        }
    }
}
#endif