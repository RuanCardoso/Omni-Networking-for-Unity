using System;
using System.Collections.Generic;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a compact delta structure for two values of type <typeparamref name="T"/>.
    /// Designed for high-performance network synchronization, it transmits only fields that
    /// have changed since the last state, using a bitmask to minimize bandwidth usage.
    /// </summary>
    /// <typeparam name="T">
    /// An unmanaged value type supported by <see cref="DataBuffer"/>.
    /// Examples: <see cref="int"/>, <see cref="float"/>, <see cref="bool"/>, etc.
    /// </typeparam>
    [Serializable, Nested]
    [DeclareHorizontalGroup("G1")]
    [DeltaSerializable]
    public struct Delta2<T> where T : unmanaged
    {
        private static readonly IEqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        /// <summary>
        /// The first value to be tracked for changes.
        /// </summary>
        [GroupNext("G1")]
        [LabelWidth(25)]
        public T a;

        /// <summary>
        /// The second value to be tracked for changes.
        /// </summary>
        [LabelWidth(25)]
        public T b;

        /// <summary>
        /// Initializes a new <see cref="Delta2{T}"/> instance with provided values.
        /// </summary>
        /// <param name="a">Initial value for <see cref="a"/>.</param>
        /// <param name="b">Initial value for <see cref="b"/>.</param>
        public Delta2(T a, T b)
        {
            this.a = a;
            this.b = b;
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
        public readonly bool Write(ref Delta2<T> lastDelta, DataBuffer finalBlock) // disposed by the caller
        {
            byte mask = 0;
            if (!Comparer.Equals(a, lastDelta.a)) mask |= 1 << 0;
            if (!Comparer.Equals(b, lastDelta.b)) mask |= 1 << 1;

            finalBlock.Write(mask);
            bool shifted = mask != 0;

            if ((mask & (1 << 0)) != 0) finalBlock.Write(a);
            if ((mask & (1 << 1)) != 0) finalBlock.Write(b);

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
        /// A new <see cref="Delta2{T}"/> representing the updated state.
        /// </returns>
        public static Delta2<T> Read(ref Delta2<T> lastDelta, DataBuffer data)
        {
            Delta2<T> result = lastDelta;
            byte mask = data.Read<byte>();
            if ((mask & (1 << 0)) != 0) result.a = data.Read<T>();
            if ((mask & (1 << 1)) != 0) result.b = data.Read<T>();
            lastDelta = result;
            return result;
        }

        public override readonly string ToString()
        {
            return $"Delta2<{typeof(T).Name}>(a: {a}, b: {b})";
        }
    }
}