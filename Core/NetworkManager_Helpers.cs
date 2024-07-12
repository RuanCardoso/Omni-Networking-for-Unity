using System;

namespace Omni.Core
{
    // The NetworkManager class is a partial class containing methods for managing network operations.
    // It utilizes the concept of "writers" and "readers" to handle network data transmission and reception efficiently.
    // The class provides an abstraction layer for network communication, ensuring streamlined and maintainable code.
    // This setup allows for easier extensions and modifications without impacting the overall network management logic.
    public partial class NetworkManager
    {
        /// <summary>
        /// Instantiates a network identity on the server for a specific network peer and serializes its data to the buffer.
        /// </summary>
        /// <param name="prefab">The prefab of the network identity to instantiate.</param>
        /// <param name="peer">The network peer for which the identity is instantiated.</param>
        /// <param name="buffer">The buffer to write identity data.</param>
        /// <param name="OnBeforeStart">An action to execute before the network identity starts, but after it has been registered.</param>
        /// <returns>The instantiated network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer InstantiateOnServer(
            NetworkIdentity prefab,
            NetworkPeer peer,
            out NetworkIdentity identity,
            Action<NetworkIdentity> OnBeforeStart = null
        )
        {
            var message = Pool.Rent();
            identity = message.InstantiateOnServer(prefab, peer, OnBeforeStart);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1>(T1 t1)
            where T1 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1, T2>(T1 t1, T2 t2)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            message.FastWrite(t2);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            message.FastWrite(t2);
            message.FastWrite(t3);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            message.FastWrite(t2);
            message.FastWrite(t3);
            message.FastWrite(t4);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            message.FastWrite(t2);
            message.FastWrite(t3);
            message.FastWrite(t4);
            message.FastWrite(t5);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer FastWrite<T1, T2, T3, T4, T5, T6>(
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            var message = Pool.Rent();
            message.FastWrite(t1);
            message.FastWrite(t2);
            message.FastWrite(t3);
            message.FastWrite(t4);
            message.FastWrite(t5);
            message.FastWrite(t6);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1>(T1 t1)
            where T1 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1, T2>(T1 t1, T2 t2)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            message.Write(t3);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            message.Write(t3);
            message.Write(t4);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            message.Write(t3);
            message.Write(t4);
            message.Write(t5);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Write<T1, T2, T3, T4, T5, T6>(
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            message.Write(t3);
            message.Write(t4);
            message.Write(t5);
            message.Write(t6);
            return message;
        }
    }
}
