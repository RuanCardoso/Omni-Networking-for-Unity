using System;
using System.Diagnostics;

namespace Omni.Core
{
    /// <summary>
    /// Attribute used to mark methods or classes that should be included in stack trace logging.
    /// </summary>
    /// <remarks>
    /// This attribute is used by the NetworkLogger to identify which stack frames should be included
    /// in detailed logging output. When applied to a method or class, it indicates that calls to that
    /// method or class should be tracked in the stack trace for debugging purposes.
    /// 
    /// The attribute is only active in the Unity Editor (UNITY_EDITOR) and is used to provide
    /// detailed debugging information about network-related operations.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class StackTraceAttribute : Attribute
    {
    }
}