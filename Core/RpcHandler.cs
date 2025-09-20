using Omni.Shared;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#pragma warning disable

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    public struct __Null__
    {
    }

    internal class RpcMethod
    {
        internal int MethodId { get; set; }
        internal string MethodName { get; set; }
        internal int ArgsCount { get; set; }
        internal bool RequiresOwnership { get; set; }
        internal Target Target { get; set; }
        internal DeliveryMode DeliveryMode { get; set; }
        internal byte SequenceChannel { get; set; }

        internal RpcMethod(int methodId, string methodName, int argsCount, bool requiresOwnership, Target target, DeliveryMode deliveryMode, byte sequenceChannel)
        {
            MethodId = methodId;
            MethodName = methodName;
            ArgsCount = argsCount;
            RequiresOwnership = requiresOwnership;
            Target = target;
            DeliveryMode = deliveryMode;
            SequenceChannel = sequenceChannel;
        }

        internal static RpcMethod Stub(int methodId, bool requiresOwnership, Target target, DeliveryMode deliveryMode, byte sequenceChannel)
        {
            return new RpcMethod(methodId, "stub", -1, requiresOwnership, target, deliveryMode, sequenceChannel);
        }
    }

    public sealed class __RpcHandler<T1, T2, T3, T4, T5>
    {
        private readonly int expectedArgsCount = -1;

        // int: method id, action: func with your parameters
        private FrozenDictionary<int, Action> T0_actionFrozen;
        private FrozenDictionary<int, Action<T1>> T1_actionFrozen;
        private FrozenDictionary<int, Action<T1, T2>> T1_T2_actionFrozen;
        private FrozenDictionary<int, Action<T1, T2, T3>> T1_T2_T3_actionFrozen;
        private FrozenDictionary<int, Action<T1, T2, T3, T4>> T1_T2_T3_T4_actionFrozen;
        private FrozenDictionary<int, Action<T1, T2, T3, T4, T5>> T1_T2_T3_T4_T5_actionFrozen;
        private FrozenDictionary<int, RpcMethod> t_methodsFrozen; // int: method id, int: args count

        internal __RpcHandler(int expectedArgsCount = -1)
        {
            this.expectedArgsCount = expectedArgsCount;
        }

        internal bool IsValid(int methodId, out int argsCount)
        {
            bool success = t_methodsFrozen.TryGetValue(methodId, out RpcMethod method);
            if (success)
            {
                argsCount = method.ArgsCount;
                return argsCount > -1;
            }

            argsCount = -1;
            return false;
        }

        internal bool IsRequiresOwnership(int methodId)
        {
            bool success = t_methodsFrozen.TryGetValue(methodId, out RpcMethod method);
            return success && method.RequiresOwnership;
        }

        internal void Rpc(int methodId)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 0)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 0."
                );
            }

            if (T0_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke();
            }
        }

        internal void Rpc(int methodId, T1 arg1)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 1)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 1."
                );
            }

            if (T1_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1);
            }
        }

        internal void Rpc(int methodId, T1 arg1, T2 arg2)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 2)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 2."
                );
            }

            if (T1_T2_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2);
            }
        }

        internal void Rpc(int methodId, T1 arg1, T2 arg2, T3 arg3)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 3)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 3."
                );
            }

            if (T1_T2_T3_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3);
            }
        }

        internal void Rpc(int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 4)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 4."
                );
            }

            if (T1_T2_T3_T4_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3, arg4);
            }
        }

        internal void Rpc(int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 5)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 5."
                );
            }

            if (T1_T2_T3_T4_T5_actionFrozen.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3, arg4, arg5);
            }
        }

        private bool IsManualRpc(ParameterInfo[] parameters)
        {
            var parameter = parameters.FirstOrDefault();
            return parameter != null && parameter.ParameterType == typeof(DataBuffer);
        }

        internal void RegisterRpcMethodHandlers<T>(object target) where T : EventAttribute
        {
            // Reflection is very slow, but it's only called once.
            // Declared only, not inherited to optimize the search.
            // Delegates are used to avoid reflection overhead, it is much faster, like a direct call.
            // works with il2cpp.

            Dictionary<int, Action> T0_action = new();
            Dictionary<int, Action<T1>> T1_action = new();
            Dictionary<int, Action<T1, T2>> T1_T2_action = new();
            Dictionary<int, Action<T1, T2, T3>> T1_T2_T3_action = new();
            Dictionary<int, Action<T1, T2, T3, T4>> T1_T2_T3_T4_action = new();
            Dictionary<int, Action<T1, T2, T3, T4, T5>> T1_T2_T3_T4_T5_action = new();
            Dictionary<int, RpcMethod> t_methods = new();

            Type type = target.GetType();
            MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bool __net_var__ = false;
            for (int i = 0; i < methodInfos.Length; i++)
            {
                MethodInfo method = methodInfos[i];
                var attributes = method.GetCustomAttributes<T>(inherit: true).ToList();
                foreach (T attr in attributes) // Rpc Attribute can be declared multiple times.
                {
                    if (attr != null)
                    {
                        var parameters = method.GetParameters();
                        if (!IsManualRpc(parameters))
                            continue;

                        int argsCount = parameters.Length;
                        if (expectedArgsCount > -1 && argsCount != expectedArgsCount)
                        {
                            ThrowParameterCountMismatch(attr, method,
                                new TargetParameterCountException("Invalid number of arguments."));
                        }

                        if (attr.Id == NetworkConstants.k_NetworkVariableRpcId) // 255 -> Reserved to Network Variables!
                        {
                            // Derived class will be responsible for calling base method.
                            // Avoid duplicated events.
                            if (__net_var__)
                                continue;

                            __net_var__ = true;
                        }

                        // Security flag:
                        bool requiresOwnership = true;
                        Target rpcTarget = Target.Auto;
                        if (attr is ServerAttribute serverAttribute)
                        {
                            requiresOwnership = serverAttribute.RequiresOwnership;
                            rpcTarget = serverAttribute.Target;
                        }

                        if (t_methods.TryAdd(attr.Id, new RpcMethod(attr.Id, method.Name, argsCount, requiresOwnership, rpcTarget, attr.DeliveryMode, attr.SequenceChannel)))
                        {
                            switch (argsCount)
                            {
                                case 0:
                                    T0_Get(target, method, attr);
                                    break;
                                case 1:
                                    T1_Get(target, method, attr);
                                    break;
                                case 2:
                                    T2_Get(target, method, attr);
                                    break;
                                case 3:
                                    T3_Get(target, method, attr);
                                    break;
                                case 4:
                                    T4_Get(target, method, attr);
                                    break;
                                case 5:
                                    T5_Get(target, method, attr);
                                    break;
                            }
                        }
                        else
                        {
                            ThrowDuplicatedEventId(attr);
                        }
                    }
                }
            }

            void T0_Get(object target, MethodInfo method, T attr)
            {
                Action func = () => { };

                try
                {
                    func = (Action)method.CreateDelegate(typeof(Action), target);

                    if (!T0_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void T1_Get(object target, MethodInfo method, T attr)
            {
                Action<T1> func = (_) => { };

                try
                {
                    func = (Action<T1>)method.CreateDelegate(typeof(Action<T1>), target);

                    if (!T1_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void T2_Get(object target, MethodInfo method, T attr)
            {
                Action<T1, T2> func = (_, _) => { };

                try
                {
                    func = (Action<T1, T2>)method.CreateDelegate(typeof(Action<T1, T2>), target);

                    if (!T1_T2_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void T3_Get(object target, MethodInfo method, T attr)
            {
                Action<T1, T2, T3> func = (_, _, _) => { };

                try
                {
                    func = (Action<T1, T2, T3>)method.CreateDelegate(typeof(Action<T1, T2, T3>), target);

                    if (!T1_T2_T3_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void T4_Get(object target, MethodInfo method, T attr)
            {
                Action<T1, T2, T3, T4> func = (_, _, _, _) => { };

                try
                {
                    func = (Action<T1, T2, T3, T4>)method.CreateDelegate(typeof(Action<T1, T2, T3, T4>), target);

                    if (!T1_T2_T3_T4_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void T5_Get(object target, MethodInfo method, T attr)
            {
                Action<T1, T2, T3, T4, T5> func = (_, _, _, _, _) => { };

                try
                {
                    func = (Action<T1, T2, T3, T4, T5>)method.CreateDelegate(typeof(Action<T1, T2, T3, T4, T5>),
                        target);

                    if (!T1_T2_T3_T4_T5_action.TryAdd(attr.Id, func))
                    {
                        ThrowDuplicatedEventId(attr);
                    }
                }
                catch (TargetParameterCountException ex)
                {
                    ThrowParameterCountMismatch(attr, func.Method, ex);
                }
                catch (ArgumentException ex)
                {
                    ThrowParameterCountMismatch(
                        attr,
                        func.Method,
                        new TargetParameterCountException(
                            "Parameter count it's fine, but type mismatch.",
                            ex
                        )
                    );
                }
            }

            void ThrowParameterCountMismatch(T attr, MethodInfo func, TargetParameterCountException ex)
            {
                var expectedArguments = string.Join(
                    ", ",
                    func.GetParameters().Select(param => param.ParameterType.Name)
                );

                string rpcName = GetRpcName(attr.Id);
                NetworkLogger.__Log__(
                    $"[RPC Delegate Error] Failed to create delegate for method '{func.Name}' (RPC name: '{rpcName}', ID: {attr.Id}). " +
                    $"Exception: {ex.Message}. " +
                    $"Expected signature: [{expectedArguments}]. " +
                    $"Check parameter types and ensure they match exactly with the delegate signature.",
                    NetworkLogger.LogType.Error
                );
            }

            void ThrowDuplicatedEventId(T attr)
            {
                string rpcName = GetRpcName(attr.Id);
                NetworkLogger.__Log__(
                    $"[RPC Configuration Error] Duplicate RPC ID '{attr.Id}' detected for method '{rpcName}'. " +
                    $"Each RPC method must have a unique ID. Check for multiple methods using ID {attr.Id} and assign unique IDs to resolve this conflict.",
                    NetworkLogger.LogType.Error
                );
            }

            // Make stubs for unused rpc ids
            for (int i = 1; i <= 255; i++)
            {
                if (!t_methods.ContainsKey(i))
                {
                    RpcMethod rpcMethod = RpcMethod.Stub(i, requiresOwnership: true, Target.Auto, DeliveryMode.ReliableOrdered, sequenceChannel: 0);
                    t_methods.TryAdd(i, rpcMethod);
                }
            }

            T0_actionFrozen = T0_action.ToFrozenDictionary();
            T1_actionFrozen = T1_action.ToFrozenDictionary();
            T1_T2_actionFrozen = T1_T2_action.ToFrozenDictionary();
            T1_T2_T3_actionFrozen = T1_T2_T3_action.ToFrozenDictionary();
            T1_T2_T3_T4_actionFrozen = T1_T2_T3_T4_action.ToFrozenDictionary();
            T1_T2_T3_T4_T5_actionFrozen = T1_T2_T3_T4_T5_action.ToFrozenDictionary();
            t_methodsFrozen = t_methods.ToFrozenDictionary();
        }

        [Conditional("OMNI_DEBUG")]
        internal void ThrowIfNoRpcMethodFound(int methodId)
        {
            if (!IsValid(methodId, out _))
            {
                string rpcName = GetRpcName(methodId);
                if (methodId != NetworkConstants.k_NetworkVariableRpcId)
                {
                    NetworkLogger.__Log__(
                        $"[RPC Invoke Error] No registered RPC method found with ID '{methodId}' (expected name: '{rpcName}'). " +
                        $"Verify that a method with this ID is properly registered and available for invocation. " +
                        $"This could happen if the method was not decorated with the appropriate RPC attribute or if the method was registered on a different object.",
                        NetworkLogger.LogType.Error
                    );
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"[Network Variable Error] No registered network variable found. " +
                        $"Verify that network variables are properly registered and available for synchronization. " +
                        $"This could happen if variables were not decorated with the [NetworkVariable] attribute or if they were registered on a different object.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
        }

        internal void SetRpcParameters(int methodId, DeliveryMode deliveryMode, Target target, byte seqChannel)
        {
            if (!t_methodsFrozen.TryGetValue(methodId, out RpcMethod method))
                return;

            if (method == null)
                return;

            method.DeliveryMode = deliveryMode;
            method.Target = target;
            method.SequenceChannel = seqChannel;
        }

        internal void GetRpcParameters(int methodId, out DeliveryMode deliveryMode, out Target target, out byte seqChannel)
        {
            deliveryMode = DeliveryMode.ReliableOrdered;
            target = Target.Auto;
            seqChannel = 0;

            if (!t_methodsFrozen.TryGetValue(methodId, out RpcMethod method))
                return;

            if (method == null)
                return;

            deliveryMode = method.DeliveryMode;
            target = method.Target;
            seqChannel = method.SequenceChannel;
        }

        internal string GetRpcName(int methodId)
        {
            if (t_methodsFrozen.TryGetValue(methodId, out RpcMethod method))
            {
                return method.MethodName;
            }

            return NetworkConstants.k_InvalidRpcName;
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.