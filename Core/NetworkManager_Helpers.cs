using System.Runtime.CompilerServices;

namespace Omni.Core
{
    // The NetworkManager class is a partial class containing methods for managing network operations.
    // It utilizes the concept of "writers" and "readers" to handle network data transmission and reception efficiently.
    // The class provides an abstraction layer for network communication, ensuring streamlined and maintainable code.
    // This setup allows for easier extensions and modifications without impacting the overall network management logic.
    public partial class NetworkManager
    {
        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1>(T1 t1) where T1 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1, T2>(T1 t1, T2 t2) where T1 : unmanaged where T2 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            var message = Pool.Rent();
            message.Write(t1);
            message.Write(t2);
            message.Write(t3);
            return message;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4) where T1 : unmanaged
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
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) where T1 : unmanaged
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
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        /// <returns>The network message. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer FastWrite<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
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