namespace Omni.Core
{
    // The NetworkManager class is a partial class containing methods for managing network operations.
    // It utilizes the concept of "writers" and "readers" to handle network data transmission and reception efficiently.
    // The class provides an abstraction layer for network communication, ensuring streamlined and maintainable code.
    // This setup allows for easier extensions and modifications without impacting the overall network management logic.
    public partial class NetworkManager
    {
        /// <summary>
        /// Instantiates a network identity on the server.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object. If not provided, a dynamic unique ID will be generated.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity InstantiateOnServer(
            NetworkIdentity prefab,
            int peerId,
            int identityId = 0
        )
        {
            if (identityId == 0)
            {
                identityId = NetworkHelper.GenerateDynamicUniqueId();
            }

            return NetworkHelper.Instantiate(prefab, Server.Peers[peerId], identityId, true, false);
        }

        /// <summary>
        /// Instantiates a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity InstantiateOnServer(NetworkIdentity prefab, NetworkPeer peer)
        {
            return InstantiateOnServer(prefab, peer.Id, 0);
        }

        /// <summary>
        /// Instantiates a network identity on the client.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who owns the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity InstantiateOnClient(
            NetworkIdentity prefab,
            int peerId,
            int identityId
        )
        {
            bool isLocalPlayer = LocalPeer.Id == peerId;
            return NetworkHelper.Instantiate(prefab, LocalPeer, identityId, false, isLocalPlayer);
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
