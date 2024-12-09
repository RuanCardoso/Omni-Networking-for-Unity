using System;
using System.Diagnostics;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class StackTraceAttribute : Attribute
    {
    }
}