using System;
using System.Collections.Generic;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a compact delta structure for four values of type <typeparamref name="T"/>.
    /// Designed for network synchronization where only modified fields are serialized,
    /// reducing bandwidth usage by leveraging a bitmask to mark changes.
    /// </summary>
    /// <typeparam name="T">
    /// An unmanaged value type supported by <see cref="DataBuffer"/>.
    /// Examples: <see cref="int"/>, <see cref="float"/>, <see cref="bool"/>, etc.
    /// </typeparam>
    [Serializable, Nested]
    [DeclareHorizontalGroup("G1")]
    [DeltaSerializable]
    public struct Delta4<T> where T : unmanaged
    {
        private static readonly IEqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        /// <summary>
        /// The first value to be tracked for changes.
        /// </summary>
        [GroupNext("G1")]
        public T a;

        /// <summary>
        /// The second value to be tracked for changes.
        /// </summary>
        public T b;

        /// <summary>
        /// The third value to be tracked for changes.
        /// </summary>
        public T c;

        /// <summary>
        /// The fourth value to be tracked for changes.
        /// </summary>
        public T d;

        /// <summary>
        /// Initializes a new instance of the <see cref="Delta4{T}"/> struct with specified values.
        /// </summary>
        /// <param name="a">The initial value for <see cref="a"/>.</param>
        /// <param name="b">The initial value for <see cref="b"/>.</param>
        /// <param name="c">The initial value for <see cref="c"/>.</param>
        /// <param name="d">The initial value for <see cref="d"/>.</param>
        public Delta4(T a, T b, T c, T d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        /// <summary>
        /// Writes changes between the current state and the <paramref name="lastDelta"/> into a <see cref="DataBuffer"/>.
        /// A bitmask is written first to indicate which fields changed.
        /// Only the changed fields are then serialized in order.
        /// </summary>
        /// <param name="lastDelta">
        /// Reference to the previous state. This value will be updated to the current state after writing.
        /// </param>
        /// <param name="finalBlock">
        /// Destination <see cref="DataBuffer"/> to write into. Ownership remains with the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one value changed and was written,
        /// otherwise <see langword="false"/>.
        /// </returns>
        public readonly bool Write(ref Delta4<T> lastDelta, DataBuffer finalBlock) // disposed by the caller
        {
            byte mask = 0;
            if (!Comparer.Equals(a, lastDelta.a)) mask |= 1 << 0;
            if (!Comparer.Equals(b, lastDelta.b)) mask |= 1 << 1;
            if (!Comparer.Equals(c, lastDelta.c)) mask |= 1 << 2;
            if (!Comparer.Equals(d, lastDelta.d)) mask |= 1 << 3;

            finalBlock.Write(mask);
            bool shifted = mask != 0;

            if ((mask & (1 << 0)) != 0) finalBlock.Write(a);
            if ((mask & (1 << 1)) != 0) finalBlock.Write(b);
            if ((mask & (1 << 2)) != 0) finalBlock.Write(c);
            if ((mask & (1 << 3)) != 0) finalBlock.Write(d);

            lastDelta = this;
            return shifted;
        }

        /// <summary>
        /// Reads and applies delta information from a <see cref="DataBuffer"/>.
        /// Fields marked in the bitmask will be updated, preserving values of unchanged fields.
        /// </summary>
        /// <param name="lastDelta">
        /// Reference to the previous state. Will be updated with the newly applied delta.
        /// </param>
        /// <param name="data">The <see cref="DataBuffer"/> containing the serialized delta.</param>
        /// <returns>
        /// A new <see cref="Delta4{T}"/> representing the updated state.
        /// </returns>
        public static Delta4<T> Read(ref Delta4<T> lastDelta, DataBuffer data)
        {
            Delta4<T> result = lastDelta;
            byte mask = data.Read<byte>();
            if ((mask & (1 << 0)) != 0) result.a = data.Read<T>();
            if ((mask & (1 << 1)) != 0) result.b = data.Read<T>();
            if ((mask & (1 << 2)) != 0) result.c = data.Read<T>();
            if ((mask & (1 << 3)) != 0) result.d = data.Read<T>();
            lastDelta = result;
            return result;
        }

        public override readonly string ToString()
        {
            return $"Delta4<{typeof(T).Name}>(a: {a}, b: {b}, c: {c}, d: {d})";
        }
    }
}