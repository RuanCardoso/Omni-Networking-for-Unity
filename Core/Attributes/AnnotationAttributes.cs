using System;

namespace Omni.Core.Attributes
{
    internal class ServerOnlyAttribute : Attribute
    {
    }

    internal class ClientOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class GenerateSecureKeysAttribute : Attribute { }
}

namespace Omni.Core
{
    /// <summary>
    /// Marks a struct as serializable for delta compression.
    /// </summary>
    /// <remarks>
    /// When applied to a struct, it enables automatic delta serialization for network transmission.
    /// Delta serialization only sends the changes (deltas) between the current and previous state,
    /// reducing bandwidth usage for networked objects.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class DeltaSerializableAttribute : Attribute
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

    /// <summary>
    /// Marks a class or struct as a database model.
    /// </summary>
    /// <remarks>
    /// When applied to a class or struct, it indicates that the type represents a database entity.
    /// This attribute enables the type to be used with database operations such as queries, inserts,
    /// updates, and deletes through the ORM system.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class ModelAttribute : Attribute
    { }

    /// <summary>
    /// Marks a field, property, or class for automatic detection and synchronization of changes to nested (inner) objects.
    /// </summary>
    /// <remarks>
    /// When applied to a field, property, or class, this attribute enables tracking of both direct assignments
    /// and changes to fields or properties inside the object. For example, both operations will be detected:
    /// <c>myClass = new MyClass();</c> and <c>myClass.health = 100f;</c>.
    /// 
    /// Without this attribute, only direct assignments to the property itself would be detected.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class NestedNetworkVariableAttribute : Attribute
    { }

    /// <summary>
    /// Marks a method to be stripped from client builds.
    /// </summary>
    /// <remarks>
    /// When applied to a method, this attribute signals the IL post-processor to remove the method
    /// from the compiled assembly in client builds. This is useful for server-only methods that
    /// should not be included in client builds, helping to reduce the client build size and
    /// prevent execution of server-specific code on clients.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StripAttribute : Attribute
    { }
}
