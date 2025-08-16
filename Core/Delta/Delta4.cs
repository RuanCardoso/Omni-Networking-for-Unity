using System;
using System.Collections.Generic;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a delta structure holding four unmanaged values of type <typeparamref name="T"/>.
    /// Used for efficient network synchronization by only transmitting changed values.
    /// </summary>
    /// <typeparam name="T">An unmanaged type to be tracked for delta changes.</typeparam>
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
        /// Writes the delta between the current and last state to a <see cref="DataBuffer"/>.
        /// Only changed values are written, using a bitmask to indicate which fields have changed.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta4{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
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
        /// Reads a delta from a <see cref="DataBuffer"/> and applies it to the last known state.
        /// Only fields indicated by the bitmask are updated.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta4{T}"/> state.</param>
        /// <param name="data">The <see cref="DataBuffer"/> containing the delta data.</param>
        /// <returns>
        /// A new <see cref="Delta4{T}"/> instance with updated values.
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