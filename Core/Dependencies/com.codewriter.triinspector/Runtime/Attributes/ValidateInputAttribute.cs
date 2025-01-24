using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ValidateInputAttribute : Attribute
    {
        public string Method { get; }

        public ValidateInputAttribute(string method)
        {
            Method = method;
        }
    }
}