using System;
using System.Diagnostics;
using Omni.Inspector;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class SerializePropertyAttribute : ShowInInspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HidePickerAttribute : HideReferencePickerAttribute
    {
    }
}