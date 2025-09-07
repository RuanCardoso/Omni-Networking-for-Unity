using Mono.CecilX;
using Mono.CecilX.Cil;
using System.Linq;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

#pragma warning disable

/**
 * NetworkVariablePatch.cs
 * 
 * This script is part of the IL Post-Processing system for Omni Networking for Unity.
 * It handles the automatic patching of network variables in user code during compilation.
 * 
 * Key responsibilities:
 * - Processes assemblies that reference Omni.Core
 * - Identifies classes with [NetworkVariable] attributes
 * - Injects synchronization code for network variables only to nested fields or properties in a class
 * - Handles nested network variables marked with [NestedNetworkVariable]
 * 
 * Usage examples:
 * 
 * nested network variable:
 *    ```csharp
 *    [NestedNetworkVariable]
 *    [MemoryPackable]
 *    [Serializable]
 *    public partial class Player
 *    {
 *        public int life;
 *    }
 *    
 *    public class ServerTests : ServerBehaviour
 *    {
 *        [NetworkVariable(CheckEquality = false)]
 *        private Player m_Player = new();
 *        
 *        void Update()
 *        {
 *            Player.life++; // Automatically synced
 *            // Inject the sync method with IL
 *        }
 *    }
 *    ```
 */

namespace Omni.ILPatch
{
    /// <summary>
    /// Implements the ILPostProcessor interface to handle network variable patching.
    /// This class injects synchronization code for network variables during compilation.
    /// It specifically targets fields marked with [NetworkVariable] attribute and handles
    /// nested network variables marked with [NestedNetworkVariable].
    /// </summary>
    internal static class NetworkVariablePatch
    {
        internal static void Process(AssemblyDefinition assembly, List<DiagnosticMessage> diagnostics)
        {
            var module = assembly.MainModule;
            foreach (var type in module.Types)
            {
                IEnumerable<(MethodDefinition Method, Instruction Instruction, int Index)> methodsToPatch = type.Methods
                    .SelectMany(m => GetInstructions(m, null)
                        .Where(i => IsNetworkVariableInstance(i, m))
                        .Select(i => (
                            Method: m,
                            Instruction: i,
                            Index: GetInstructions(m, diagnostics).IndexOf(i) - 1 // ldarg/ldarg_0
                        ))
                    );

                foreach (var (method, fInstruction, fCIndex) in methodsToPatch)
                {
                    var ilProcessor = method.Body.GetILProcessor();
                    ilProcessor.Deoptimize();

                    var instructions = GetInstructions(method, diagnostics);
                    if (method.IsSetter || method.IsGetter)
                        continue;

                    if (fInstruction.Operand is not IMemberDefinition fMember)
                        continue;

                    fMember = fMember.ResolveDefinition(out bool fIsSetter);
                    if (fIsSetter)
                        continue;

                    string sync_method_name = fMember.Name;
                    if (sync_method_name.StartsWith("m_"))
                        sync_method_name = sync_method_name.Substring(2);
                    sync_method_name = $"Sync{sync_method_name}";

                    MethodDefinition sync_method = type.FindMethodInHierarchy(sync_method_name);
                    if (sync_method == null)
                        continue;

                    if (method.Name == sync_method.Name)
                        continue;

                    int currentIndex = fCIndex + 2; // skip current ldarg/ldarg_0 and call/ldfld
                    Instruction nextInstance = instructions.Skip(currentIndex).FirstOrDefault(x => IsNetworkVariableInstance(x, method));
                    int nextInstanceIndex = nextInstance != null ? instructions.IndexOf(nextInstance) - 2 : instructions.Count; // skip current call/ldfld
                    for (int i = currentIndex; i <= nextInstanceIndex; i++) // analyze the block between current and next instance
                    {
                        if (i >= instructions.Count)
                            continue;

                        Instruction currentInstruction = instructions[i];
                        IMemberDefinition currMember = currentInstruction.Operand as IMemberDefinition;
                        if (currMember == null)
                        {
                            if (IsStStoreInstruction(currentInstruction))
                            {
                                var nextInstruction = fInstruction.Next;
                                if (nextInstruction != null && (nextInstruction.OpCode == OpCodes.Ldfld || nextInstruction.OpCode == OpCodes.Ldflda) || nextInstruction.OpCode == OpCodes.Call)
                                {
                                    currMember = nextInstruction.Operand as IMemberDefinition;
                                }
                            }
                        }

                        bool isGenericCollection = false;
                        if (currMember == null)
                        {
                            if (currentInstruction.Operand is MethodReference methodReference)
                            {
                                string fullName = methodReference.FullName;
                                if (fullName.Contains("System.Collections.Generic") || fullName.Contains("Omni.Collections"))
                                {
                                    currMember = methodReference.Resolve();
                                    isGenericCollection = true;
                                }
                            }
                        }

                        if (currMember == null)
                            continue;

                        currMember = currMember.ResolveDefinition(out bool isSetter);
                        if (IsStStoreInstruction(currentInstruction) || currentInstruction.OpCode == OpCodes.Stfld || currentInstruction.OpCode == OpCodes.Callvirt || currentInstruction.OpCode == OpCodes.Call)
                        {
                            if (!isGenericCollection)
                            {
                                if ((currentInstruction.OpCode == OpCodes.Callvirt || currentInstruction.OpCode == OpCodes.Call) && !isSetter)
                                    continue;
                            }

                            var declaringType = currMember.DeclaringType;
                            if (isGenericCollection)
                            {
                                if (fMember is FieldDefinition fGenDef)
                                    declaringType = fGenDef.FieldType.Resolve();
                                else if (fMember is PropertyDefinition fGenProp)
                                    declaringType = fGenProp.PropertyType.Resolve();
                            }

                            if (declaringType.HasAttribute("NestedNetworkVariableAttribute") || currMember.HasAttribute("NestedNetworkVariableAttribute"))
                            {
                                bool isReferenceType = !declaringType.IsValueType;
                                if (isReferenceType)
                                {
                                    if (fMember is not PropertyDefinition fDefProp)
                                    {
                                        ILPatchHelper.OutputDebugString($"NetworkVariable: Reference types must be accessed through their property definition for proper network synchronization. Member: {fMember.FullName}, Type: {fMember.DeclaringType.FullName}", diagnostics, DiagnosticType.Error);
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (fMember is not FieldDefinition fDefField)
                                    {
                                        ILPatchHelper.OutputDebugString($"NetworkVariable: Value types must be accessed through their field definition for proper network synchronization. Member: {fMember.FullName}, Type: {fMember.DeclaringType.FullName}", diagnostics, DiagnosticType.Error);
                                        continue;
                                    }
                                }

                                var ldarg = ilProcessor.Create(OpCodes.Ldarg_0);
                                var ldnull = ilProcessor.Create(OpCodes.Ldnull);
                                var call = ilProcessor.Create(OpCodes.Call, sync_method);

                                ilProcessor.InsertAfter(currentInstruction, ldarg);
                                ilProcessor.InsertAfter(ldarg, ldnull);
                                ilProcessor.InsertAfter(ldnull, call);
                                //ILPatchHelper.OutputDebugString($"NetworkVariable: {method.FullName} patched between index {currentIndex} and {nextInstanceIndex}", diagnostics);
                            }
                        }
                    }

                    ilProcessor.Optimize();
                }
            }
        }

        private static bool IsStStoreInstruction(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Stind_I1 // byte
                || instruction.OpCode == OpCodes.Stind_I2 // short
                || instruction.OpCode == OpCodes.Stind_I4 // int
                || instruction.OpCode == OpCodes.Stind_I8 // long
                || instruction.OpCode == OpCodes.Stind_R4 // float
                || instruction.OpCode == OpCodes.Stind_R8 // double
                || instruction.OpCode == OpCodes.Stind_Ref; // object
        }

        private static bool IsNetworkVariableInstance(Instruction i, MethodDefinition method)
        {
            if ((i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) && method.IsSetter) // ignore setter calls, instance is stored in the getter
                return false;

            bool isNetworkVariableInstance = i.Operand is IMemberDefinition member
                && member.HasAttribute("NetworkVariableAttribute")
                && (i.OpCode == OpCodes.Call || (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Ldflda)); // ref: Ldfld, value types: ldflda

            if (isNetworkVariableInstance)
            {
                if (!ILPatchHelper.IsInheritFromBase(method.DeclaringType, "ServerBehaviour", "ClientBehaviour", "NetworkBehaviour"))
                    return false;
            }

            bool previousIsLdarg_0 = i.Previous != null && i.Previous.OpCode == OpCodes.Ldarg_0;
            bool previousIsLdarg = i.Previous != null
                    && i.Previous.OpCode == OpCodes.Ldarg
                    && i.Previous.Operand is ParameterDefinition pDef
                    && pDef.Method.HasThis;

            return isNetworkVariableInstance && (previousIsLdarg_0 || previousIsLdarg);
        }

        private static List<Instruction> GetInstructions(MethodDefinition method, List<DiagnosticMessage> diagnostics)
        {
            if (!IsValidMethod(method, diagnostics))
                return new List<Instruction>();

            return method.Body.Instructions.ToList();
        }

        private static bool IsValidMethod(MethodDefinition method, List<DiagnosticMessage> diagnostics)
        {
            if (!method.HasBody)
                return false;

            if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime)
                return false;

            if (method.ReturnType.IsByReference)
                return false;

            if (method.IsConstructor) // Use the Start() or Awake() for initialization
                return false;

            if (method.IsStatic)
                return false;

            if (method.HasGenericParameters || method.IsGenericInstance)
                return false;

            if (method.Name.StartsWith("_"))
                return false;

            return true;
        }
    }
}