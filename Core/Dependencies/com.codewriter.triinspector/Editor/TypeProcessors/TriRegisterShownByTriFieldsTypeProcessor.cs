﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Omni.Inspector.Utilities;
using Omni.Inspector;
using Omni.Inspector.TypeProcessors;

[assembly: RegisterTriTypeProcessor(typeof(TriRegisterShownByTriFieldsTypeProcessor), 1)]

namespace Omni.Inspector.TypeProcessors
{
    public class TriRegisterShownByTriFieldsTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            const int fieldsOffset = 5001;

            properties.AddRange(TriReflectionUtilities
                .GetAllInstanceFieldsInDeclarationOrder(type)
                .Where(IsSerialized)
                .Select((it, ind) => TriPropertyDefinition.CreateForFieldInfo(ind + fieldsOffset, it)));
        }

        private static bool IsSerialized(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttribute<ShowInInspectorAttribute>(false) != null &&
                   TriUnitySerializationUtilities.IsSerializableByUnity(fieldInfo) == false;
        }
    }
}