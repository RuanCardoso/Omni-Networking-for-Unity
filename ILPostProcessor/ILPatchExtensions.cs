using System.Collections.Generic;
using System.Linq;
using Mono.CecilX;
using Mono.CecilX.Cil;
using Mono.CecilX.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Omni.ILPatch
{
    internal static class ILPatchExtensions
    {
        internal static bool HasAttribute(this IMemberDefinition memberDefinition, string attributeName)
        {
            var resolvedDefinition = memberDefinition.ResolveDefinition(out _);
            return resolvedDefinition.CustomAttributes.Any(attr => attr.AttributeType.Name == attributeName);
        }

        internal static MethodDefinition FindMethodInHierarchy(this TypeDefinition type, string methodName)
        {
            var currentType = type;
            while (currentType != null)
            {
                try
                {
                    var method = currentType.Methods.FirstOrDefault(m => m.Name == methodName);
                    if (method != null)
                        return method;
                    currentType = currentType?.BaseType?.Resolve();
                }
                catch
                {
                    break;
                }
            }

            return null;
        }

        internal static IMemberDefinition ResolveDefinition(this IMemberDefinition memberDefinition, out bool isSetter)
        {
            isSetter = false;
            if (memberDefinition is MethodDefinition method)
            {
                if (method.IsSetter || method.IsGetter)
                {
                    var currentType = method.DeclaringType;
                    while (currentType != null)
                    {
                        try
                        {
                            var property = currentType.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
                            if (property != null)
                            {
                                isSetter = property.SetMethod == method;
                                return property;
                            }

                            currentType = currentType?.BaseType?.Resolve();
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }

            return memberDefinition;
        }

        internal static void Deoptimize(this ILProcessor ilProcessor)
        {
            ilProcessor.Body.SimplifyMacros();
        }

        internal static void Optimize(this ILProcessor ilProcessor)
        {
            ilProcessor.Body.OptimizeMacros();
        }
    }
}