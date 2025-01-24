using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    [Conditional("UNITY_EDITOR")]
    public class DrawWithTriInspectorAttribute : Attribute
    {
    }
}