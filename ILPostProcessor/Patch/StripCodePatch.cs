using Mono.CecilX;
using Mono.CecilX.Cil;
using System.Linq;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using System;

#pragma warning disable

namespace Omni.ILPatch
{
    internal static class StripCodePatch
    {
        private const string AttributeName = "StripFromClientAttribute";

        internal static void Process(AssemblyDefinition assembly, List<DiagnosticMessage> diagnostics)
        {
            var module = assembly.MainModule;
            foreach (var type in module.Types)
            {
                StripMethods(type);
                StripField(type, diagnostics);
            }
        }

        private static void StripField(TypeDefinition dType, List<DiagnosticMessage> diagnostics)
        {
            IEnumerable<MethodDefinition> ctors = dType.Methods
                .Where(m => m.IsConstructor && !m.IsStatic && m.HasBody)
                .Where(m => m.Body.Instructions.Any(i =>
                    i.OpCode == OpCodes.Stfld &&
                    i.Operand is FieldReference fr &&
                    fr.Resolve().HasAttribute(AttributeName))); // Optimized search

            var handlers = new Dictionary<Code, Action<Instruction>>
            {
                { Code.Ldstr,  i => i.Operand = "" }, // string
                { Code.Ldc_I4, i => i.Operand = 0 }, // [int, bool, char, short, byte]
                { Code.Ldc_I8, i => i.Operand = 0L }, // [long, ulong]
                { Code.Ldc_R4, i => i.Operand = 0f }, // [float]
                { Code.Ldc_R8, i => i.Operand = 0d }, // [double]
                { Code.Newobj, i =>
                    {
                        i.OpCode = OpCodes.Ldnull;
                        i.Operand = null;
                    }
                }
            };

            foreach (var ctor in ctors)
            {
                var il = ctor.Body.GetILProcessor();
                il.Deoptimize();

                // Search for stfld
                var instructions = il.Body.Instructions.ToList();
                for (int i = 0; i < instructions.Count; i++)
                {
                    var cInstruction = instructions[i];
                    if (cInstruction.Operand is FieldReference fieldRef)
                    {
                        var fieldDef = fieldRef.Resolve();
                        if (fieldDef.HasAttribute(AttributeName))
                        {
                            if (cInstruction.OpCode == OpCodes.Stfld) // Field assignment
                            {
                                var isMatch = false;
                                var pInstruction = cInstruction.Previous;
                                while (pInstruction.OpCode != OpCodes.Ldarg) // Find ldarg
                                {
                                    if (pInstruction.OpCode == OpCodes.Ldtoken && pInstruction.Operand is FieldReference fr)
                                    {
                                        var ldDef = fr.Resolve();
                                        if (ldDef.InitialValue != null)
                                            ldDef.InitialValue = new byte[ldDef.InitialValue.Length];

                                        isMatch = true;
                                        break;
                                    }
                                    else
                                    {
                                        if (handlers.TryGetValue(pInstruction.OpCode.Code, out var setOperand))
                                        {
                                            isMatch = true;
                                            setOperand(pInstruction);
                                            break;
                                        }
                                    }

                                    pInstruction = pInstruction.Previous;
                                }

                                if (!isMatch)
                                {
                                    ILPatchHelper.OutputDebugString($"The attribute [StripFromClient] is not supported for the field {fieldDef.Name} in the type {fieldDef.DeclaringType.Name}.", diagnostics, DiagnosticType.Error);
                                }
                            }
                        }
                    }
                }

                il.Optimize();
            }
        }

        private static void StripMethods(TypeDefinition type)
        {
            IEnumerable<MethodDefinition> methods = type.Methods
                .Where(m => m.HasAttribute(AttributeName));

            foreach (var method in methods)
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