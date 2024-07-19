// Roslyn Generated //  Roslyn Analyzer
using System;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Auto-syncing a property with a Memory Pack serializer on the network.
    /// </summary>
    /// <remarks>
    /// Applies a unique identifier to a property for network synchronization.
    /// </remarks>
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
