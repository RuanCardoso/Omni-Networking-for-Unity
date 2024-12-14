using System;
using TriInspector;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SerializeProperty : ShowInInspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HidePicker : HideReferencePickerAttribute
    {
    }
}