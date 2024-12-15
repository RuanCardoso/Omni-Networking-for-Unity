using System;
using System.Diagnostics;
using TriInspector;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class SerializeProperty : ShowInInspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HidePicker : HideReferencePickerAttribute
    {
    }
}