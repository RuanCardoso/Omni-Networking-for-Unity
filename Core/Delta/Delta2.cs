using System;
using System.Collections.Generic;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a delta structure holding two unmanaged values of type <typeparamref name="T"/>.
    /// Used for efficient network synchronization by only transmitting changed values.
    /// </summary>
    /// <typeparam name="T">An unmanaged type to be tracked for delta changes.</typeparam>
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
        /// Initializes a new instance of the <see cref="Delta2{T}"/> struct with specified values.
        /// </summary>
        /// <param name="a">The initial value for <see cref="a"/>.</param>
        /// <param name="b">The initial value for <see cref="b"/>.</param>
        public Delta2(T a, T b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Writes the delta between the current and last state to a <see cref="DataBuffer"/>.
        /// Only changed values are written, using a bitmask to indicate which fields have changed.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta2{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
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
        /// Reads a delta from a <see cref="DataBuffer"/> and applies it to the last known state.
        /// Only fields indicated by the bitmask are updated.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta2{T}"/> state.</param>
        /// <param name="data">The <see cref="DataBuffer"/> containing the delta data.</param>
        /// <returns>
        /// A new <see cref="Delta2{T}"/> instance with updated values.
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