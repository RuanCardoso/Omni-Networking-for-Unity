using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    [Conditional("UNITY_EDITOR")]
    public class NestedAttribute : Attribute
    {

    }
}