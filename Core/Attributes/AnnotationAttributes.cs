using System;

namespace Omni.Core.Attributes
{
    internal class ServerOnlyAttribute : Attribute
    {
    }

    internal class ClientOnlyAttribute : Attribute
    {
    }
}

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class DeltaSerializable : Attribute
    {
        /// <summary>
        /// Marks a struct as serializable for delta compression.
        /// When applied to a struct, it enables automatic delta serialization for network transmission.
        /// </summary>
        /// <remarks>
        /// Delta serialization only sends the changes (deltas) between the current and previous state,
        /// reducing bandwidth usage for networked objects.
        /// If Enabled is set to false, the entire structure will be sent without compression (full serialization).
        /// </remarks>
        public bool Enabled { get; set; } = true;
    }
}
