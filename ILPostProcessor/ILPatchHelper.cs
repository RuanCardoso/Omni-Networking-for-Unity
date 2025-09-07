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
    }
}