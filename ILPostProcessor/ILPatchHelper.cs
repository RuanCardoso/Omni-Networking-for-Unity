using System;
using System.Collections.Generic;
using System.Linq;
using Mono.CecilX;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Omni.ILPatch
{
    internal static class ILPatchHelper
    {
        internal static void OutputDebugString(string message, List<DiagnosticMessage> diagnostics, DiagnosticType diagnosticType = DiagnosticType.Warning)
        {
            if (diagnostics == null)
                return;

            diagnostics.Add(new DiagnosticMessage()
            {
                DiagnosticType = diagnosticType,
                MessageData = message
            });
        }

        internal static bool IsInheritFromBase(TypeDefinition type, params string[] baseNames)
        {
            return baseNames.Any(baseName => IsInheritFromBase(type, baseName));
        }

        internal static bool IsInheritFromBase(TypeDefinition type, string baseName)
        {
            var currentType = type;
            while (currentType != null)
            {
                try
                {
                    if (currentType.BaseType.Name == baseName)
                        return true;

                    currentType = currentType?.BaseType?.Resolve();
                }
                catch
                {
                    break;
                }
            }

            return false;
        }

        internal static bool IsValidMethod(MethodDefinition method, List<DiagnosticMessage> diagnostics)
        {
            if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime)
            {
                OutputDebugString($"Skipping method {method.FullName} because it is abstract, extern, or runtime.", diagnostics);
                return false;
            }

            if (method.ReturnType.IsByReference)
            {
                OutputDebugString($"Skipping method {method.FullName} because it returns a by-reference type.", diagnostics);
                return false;
            }

            if (method.IsConstructor)
            {
                OutputDebugString($"Skipping method {method.FullName} because it is a constructor.", diagnostics);
                return false;
            }

            if (method.IsStatic)
            {
                OutputDebugString($"Skipping method {method.FullName} because it is static.", diagnostics);
                return false;
            }

            if (method.HasGenericParameters || method.IsGenericInstance)
            {
                OutputDebugString($"Skipping method {method.FullName} because it is generic.", diagnostics);
                return false;
            }

            if (method.Name.StartsWith("_"))
            {
                OutputDebugString($"Skipping method {method.FullName} because it is internal.", diagnostics);
                return false;
            }

            if (!method.HasBody)
                return false;

            return true;
        }
    }
}