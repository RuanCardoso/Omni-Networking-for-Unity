using Mono.CecilX;
using Mono.CecilX.Cil;
using System.Linq;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

#pragma warning disable

namespace Omni.ILPatch
{
    internal static class StripCodePatch
    {
        internal static void Process(AssemblyDefinition assembly, List<DiagnosticMessage> diagnostics)
        {
            var module = assembly.MainModule;
            foreach (var type in module.Types)
            {
                IEnumerable<MethodDefinition> methodsToPatch = type.Methods.Where(m => m.HasAttribute("StripAttribute"));
                foreach (var method in methodsToPatch)
                {
                    if (!ILPatchHelper.IsValidMethod(method, null))
                        continue;

                    var il = method.Body.GetILProcessor();
                    il.Body.Variables.Clear();
                    il.Body.Instructions.Clear();
                    il.Body.ExceptionHandlers.Clear();
                    il.Body.InitLocals = false;
                    il.Body.MaxStackSize = 1;

                    if (method.ReturnType.FullName == "System.Void")
                    {
                        il.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        if (method.ReturnType.IsValueType)
                        {
                            il.Body.InitLocals = true;
                            var tempVar = new VariableDefinition(method.ReturnType);
                            il.Body.Variables.Add(tempVar);
                            il.Emit(OpCodes.Ldloca_S, tempVar);
                            il.Emit(OpCodes.Initobj, method.ReturnType);
                            il.Emit(OpCodes.Ldloc_S, tempVar);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldnull);
                        }

                        il.Emit(OpCodes.Ret);
                    }
                }
            }
        }
    }
}