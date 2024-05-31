using System;

namespace Omni.Core
{
    public class EventAttribute : Attribute
    {
        internal byte Id { get; }

        public EventAttribute(byte id)
        {
            Id = id;
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
