using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Omni.Shared;

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    internal struct Null { }

    internal sealed class InvokeBehaviour<T1, T2, T3, T4, T5>
    {
        private readonly int expectedArgsCount = -1;

        // int: method id, action: func with your parameters
        private readonly Dictionary<int, Action> T0_action = new();
        private readonly Dictionary<int, Action<T1>> T1_action = new();
        private readonly Dictionary<int, Action<T1, T2>> T1_T2_action = new();
        private readonly Dictionary<int, Action<T1, T2, T3>> T1_T2_T3_action = new();
        private readonly Dictionary<int, Action<T1, T2, T3, T4>> T1_T2_T3_T4_action = new();
        private readonly Dictionary<int, Action<T1, T2, T3, T4, T5>> T1_T2_T3_T4_T5_action = new();
        private readonly Dictionary<int, int> t_methods = new(); // int: method id, int: args count

        internal InvokeBehaviour(int expectedArgsCount = -1)
        {
            this.expectedArgsCount = expectedArgsCount;
        }

        internal bool Exists(int methodId, out int argsCount)
        {
            return t_methods.TryGetValue(methodId, out argsCount);
        }

        internal void Invoke(int methodId)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 0)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 0."
                );
            }

            if (T0_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke();
            }
        }

        internal void Invoke(int methodId, T1 arg1)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 1)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 1."
                );
            }

            if (T1_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1);
            }
        }

        internal void Invoke(int methodId, T1 arg1, T2 arg2)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 2)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 2."
                );
            }

            if (T1_T2_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2);
            }
        }

        internal void Invoke(int methodId, T1 arg1, T2 arg2, T3 arg3)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 3)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 3."
                );
            }

            if (T1_T2_T3_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3);
            }
        }

        internal void Invoke(int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 4)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 4."
                );
            }

            if (T1_T2_T3_T4_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3, arg4);
            }
        }

        internal void Invoke(int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (expectedArgsCount > -1 && expectedArgsCount != 5)
            {
                throw new Exception(
                    $"Invalid number of arguments: {expectedArgsCount} instead of 5."
                );
            }

            if (T1_T2_T3_T4_T5_action.TryGetValue(methodId, out var action))
            {
                action?.Invoke(arg1, arg2, arg3, arg4, arg5);
            }
        }

        internal void FindEvents<T>(object target, BindingFlags flags)
            where T : EventAttribute
        {
            // Reflection is very slow, but it's only called once.
            // Declared only, not inherited to optimize the search.
            // Delegates are used to avoid reflection overhead, it is much faster, like a direct call.
            // works with il2cpp.

            Type type = target.GetType();
            MethodInfo[] methodInfos = type.GetMethods((System.Reflection.BindingFlags)flags);
            bool __net_var__ = false;
            for (int i = 0; i < methodInfos.Length; i++)
            {
                MethodInfo method = methodInfos[i];
                var attributes = method.GetCustomAttributes<T>().ToList();
                foreach (T attr in attributes)
                {
                    if (attr != null)
                    {
                        int argsCount = method.GetParameters().Length;
                        if (expectedArgsCount > -1 && argsCount != expectedArgsCount)
                        {
                            ThrowParameterCountMismatch(
                                attr,
                                method,
                                new TargetParameterCountException("Invalid number of arguments.")
                            );
                        }

                        if (attr.Id == NetworkConstants.NET_VAR_RPC_ID) // 255 -> Reserved to Network Variables!
                        {
                            // Derived class will be responsible of calling base method.
                            // Avoid duplicated events.
                            if (__net_var__)
                                continue;

                            __net_var__ = true;
                        }

                        if (t_methods.TryAdd(attr.Id, argsCount))
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
                    func =
                        (Action<T1, T2, T3>)
                            method.CreateDelegate(typeof(Action<T1, T2, T3>), target);

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
                    func =
                        (Action<T1, T2, T3, T4>)
                            method.CreateDelegate(typeof(Action<T1, T2, T3, T4>), target);

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
                    func =
                        (Action<T1, T2, T3, T4, T5>)
                            method.CreateDelegate(typeof(Action<T1, T2, T3, T4, T5>), target);

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

            void ThrowParameterCountMismatch(
                T attr,
                MethodInfo func,
                TargetParameterCountException ex
            )
            {
                var expectedArguments = string.Join(
                    ", ",
                    func.GetParameters().Select(param => param.ParameterType.Name)
                );

                NetworkLogger.__Log__(
                    $"Delegate Creation Error: Failed to create delegate for method '{func.Name}' with event attribute ID '{attr.Id}'. "
                        + $"Exception: {ex.Message}. Expected arguments: [{expectedArguments}].",
                    NetworkLogger.LogType.Error
                );
            }

            void ThrowDuplicatedEventId(T attr)
            {
                NetworkLogger.__Log__(
                    $"Configuration Error: Duplicated event ID '{attr.Id}'. "
                        + $"Ensure that each method with the event attribute has a unique ID.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        [Conditional("OMNI_DEBUG")]
        internal void ThrowNoMethodFound(int methodId)
        {
            if (!Exists(methodId, out _))
            {
                NetworkLogger.__Log__(
                    $"Invoke Error: No method(event) with ID '{methodId}' registered. Ensure a method with the same ID is registered.",
                    NetworkLogger.LogType.Error
                );
            }
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
