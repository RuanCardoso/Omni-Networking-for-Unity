﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Omni.Inspector.Utilities;
using Omni.Inspector;
using Omni.Inspector.TypeProcessors;

[assembly: RegisterTriTypeProcessor(typeof(TriRegisterButtonsTypeProcessor), 3)]

namespace Omni.Inspector.TypeProcessors
{
    public class TriRegisterButtonsTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            const int methodsOffset = 20001;

            properties.AddRange(TriReflectionUtilities
                .GetAllInstanceMethodsInDeclarationOrder(type)
                .Where(IsSerialized)
                .Select((it, ind) => TriPropertyDefinition.CreateForMethodInfo(ind + methodsOffset, it)));
        }

        private static bool IsSerialized(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttribute<ButtonAttribute>(false) != null;
        }
    }
}