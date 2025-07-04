using System;
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
    [DeclareHorizontalGroup("G2")]
    [DeltaSerializable]
    public struct Delta8<T> where T : unmanaged
    {
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
        /// The fifth value to be tracked for changes.
        /// </summary>
        [GroupNext("G2")]
        public T e;

        /// <summary>
        /// The sixth value to be tracked for changes.
        /// </summary>
        public T f;

        /// <summary>
        /// The seventh value to be tracked for changes.
        /// </summary>
        public T g;

        /// <summary>
        /// The seventh value to be tracked for changes.
        /// </summary>
        public T h;

        /// <summary>
        /// Initializes a new instance of the <see cref="Delta4{T}"/> struct with specified values.
        /// </summary>
        /// <param name="a">The initial value for <see cref="a"/>.</param>
        /// <param name="b">The initial value for <see cref="b"/>.</param>
        /// <param name="c">The initial value for <see cref="c"/>.</param>
        /// <param name="d">The initial value for <see cref="d"/>.</param>
        /// <param name="e">The initial value for <see cref="e"/>.</param>
        /// <param name="f">The initial value for <see cref="f"/>.</param>
        /// <param name="g">The initial value for <see cref="g"/>.</param>
        /// <param name="h">The initial value for <see cref="h"/>.</param>
        public Delta8(T a, T b, T c, T d, T e, T f, T g, T h)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
            this.g = g;
            this.h = h;
        }

        /// <summary>
        /// Writes the delta between the current and last state to a <see cref="DataBuffer"/>.
        /// Only changed values are written, using a bitmask to indicate which fields have changed.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta4{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
        /// </returns>
        public readonly DataBuffer Write(ref Delta8<T> lastDelta)
        {
            return Write(ref lastDelta, out _);
        }

        /// <summary>
        /// Writes the delta between the current and last state to a <see cref="DataBuffer"/>.
        /// Only changed values are written, using a bitmask to indicate which fields have changed.
        /// </summary>
        /// <param name="lastDelta">A reference to the previous <see cref="Delta4{T}"/> state.</param>
        /// <returns>
        /// A <see cref="DataBuffer"/> containing the bitmask and any changed values. The caller is responsible for disposing the buffer.
        /// </returns>
        public readonly DataBuffer Write(ref Delta8<T> lastDelta, out bool changed) // disposed by the caller
        {
            changed = false;
            var data = NetworkManager.Pool.Rent();
            byte mask = 0;

            if (!a.Equals(lastDelta.a))
                mask |= 1 << 0;
            if (!b.Equals(lastDelta.b))
                mask |= 1 << 1;
            if (!c.Equals(lastDelta.c))
                mask |= 1 << 2;
            if (!d.Equals(lastDelta.d))
                mask |= 1 << 3;
            if (!e.Equals(lastDelta.e))
                mask |= 1 << 4;
            if (!f.Equals(lastDelta.f))
                mask |= 1 << 5;
            if (!g.Equals(lastDelta.g))
                mask |= 1 << 6;
            if (!h.Equals(lastDelta.h))
                mask |= 1 << 7;

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
            if ((mask & (1 << 2)) != 0)
            {
                data.Write(c);
                changed = true;
            }
            if ((mask & (1 << 3)) != 0)
            {
                data.Write(d);
                changed = true;
            }
            if ((mask & (1 << 4)) != 0)
            {
                data.Write(e);
                changed = true;
            }
            if ((mask & (1 << 5)) != 0)
            {
                data.Write(f);
                changed = true;
            }
            if ((mask & (1 << 6)) != 0)
            {
                data.Write(g);
                changed = true;
            }
            if ((mask & (1 << 7)) != 0)
            {
                data.Write(h);
                changed = true;
            }

            lastDelta = this;
            return data;
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
        public static Delta8<T> Read(ref Delta8<T> lastDelta, DataBuffer data)
        {
            Delta8<T> result = lastDelta;
            byte mask = data.Read<byte>();
            if ((mask & (1 << 0)) != 0)
                result.a = data.Read<T>();
            if ((mask & (1 << 1)) != 0)
                result.b = data.Read<T>();
            if ((mask & (1 << 2)) != 0)
                result.c = data.Read<T>();
            if ((mask & (1 << 3)) != 0)
                result.d = data.Read<T>();
            if ((mask & (1 << 4)) != 0)
                result.e = data.Read<T>();
            if ((mask & (1 << 5)) != 0)
                result.f = data.Read<T>();
            if ((mask & (1 << 6)) != 0)
                result.g = data.Read<T>();
            if ((mask & (1 << 7)) != 0)
                result.h = data.Read<T>();

            lastDelta = result;
            return result;
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Delta8<T> other)
                return a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d) && e.Equals(other.e) && f.Equals(other.f) && g.Equals(other.g) && h.Equals(other.h);

            return false;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d, e, f, g, h);
        }

        public override readonly string ToString()
        {
            return $"Delta8<{typeof(T).Name}>(a: {a}, b: {b}, c: {c}, d: {d}, e: {e}, f: {f}, g: {g}, h: {h})";
        }
    }
}