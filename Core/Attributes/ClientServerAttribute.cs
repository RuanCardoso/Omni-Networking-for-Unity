using System;

namespace Omni.Core
{
    public class EventAttribute : Attribute
    {
        internal byte Id { get; }

        public EventAttribute(byte id)
        {
            Id = id;
            if (Id >= 230)
            {
                throw new Exception(
                    "Event ID must be less than 230. IDs above 230 are reserved for internal use, such as RPC or custom messages. Please avoid using IDs above this threshold."
                );
            }
        }
    }

    public class ClientAttribute : EventAttribute
    {
        public ClientAttribute(byte id)
            : base(id) { }
    }

    public class ServerAttribute : EventAttribute
    {
        public ServerAttribute(byte id)
            : base(id) { }
    }
}
