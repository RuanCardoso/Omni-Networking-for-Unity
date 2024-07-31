using System;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EventAttribute : Attribute
    {
        internal byte Id { get; }

        public EventAttribute(byte id)
        {
            Id = id;
            if (Id > 230 && Id < 255) // 255: NetVar
            {
                throw new Exception(
                    $"Event ID({Id}) must be less than 230. IDs above 230 are reserved for internal use, such as RPC or custom messages. Please avoid using IDs above this threshold."
                );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClientAttribute : EventAttribute
    {
        public ClientAttribute(byte id)
            : base(id) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ServerAttribute : EventAttribute
    {
        public ServerAttribute(byte id)
            : base(id) { }
    }
}
