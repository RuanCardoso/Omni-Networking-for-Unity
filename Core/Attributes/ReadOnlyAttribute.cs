using System;
using UnityEngine;

namespace Omni.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute() { }
    }
}
