using System;
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
        /// <param name="lastDelta">A reference to the previous <see cref="Delta4{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
        /// </returns>
        public readonly DataBuffer Write(ref Delta2<T> lastDelta)
        {
            return Write(ref lastDelta, out _);
        }

        /// <summary>
        /// Writes the delta between the current and last state to a <see cref="DataBuffer"/>.
        /// Only changed values are written, using a bitmask to indicate which fields have changed.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta2{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
        /// </returns>
        public readonly DataBuffer Write(ref Delta2<T> lastDelta, out bool changed) // disposed by the caller
        {
            changed = false;
            var data = NetworkManager.Pool.Rent();
            byte mask = 0;

            if (!a.Equals(lastDelta.a))
                mask |= 1 << 0;
            if (!b.Equals(lastDelta.b))
                mask |= 1 << 1;

            data.Write(mask);

            if ((mask & (1 << 0)) != 0)
            {
                data.Write(a);
                changed = true;
            }
            if ((mask & (1 << 1)) != 0)
            {
                data.Write(b);
                changed = true;
            }

            lastDelta = this;
            return data;
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
            if ((mask & (1 << 0)) != 0)
                result.a = data.Read<T>();
            if ((mask & (1 << 1)) != 0)
                result.b = data.Read<T>();

            lastDelta = result;
            return result;
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Delta2<T> other)
                return a.Equals(other.a) && b.Equals(other.b);

            return false;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(a, b);
        }

        public override readonly string ToString()
        {
            return $"Delta2<{typeof(T).Name}>(a: {a}, b: {b})";
        }
    }
}