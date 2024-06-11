// Roslyn Generated //  Roslyn Analyzer
using System;
using UnityEngine;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NetVarAttribute : PropertyAttribute
    {
        internal byte Id { get; }

        public NetVarAttribute(byte id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AutoPropertyAttribute : PropertyAttribute { }
}
