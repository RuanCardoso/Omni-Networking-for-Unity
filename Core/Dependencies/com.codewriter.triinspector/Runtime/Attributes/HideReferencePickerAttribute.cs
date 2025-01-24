using System;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideReferencePickerAttribute : Attribute
    {
    }
}