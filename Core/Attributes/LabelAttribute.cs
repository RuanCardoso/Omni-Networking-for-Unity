using System;
using UnityEngine;

namespace Omni.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; }

        public LabelAttribute(string label)
        {
            Label = label;
        }
    }
}
