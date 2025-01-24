using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public class DrawWithUnityAttribute : Attribute
    {
        public bool WithUiToolkit { get; set; }
    }
}