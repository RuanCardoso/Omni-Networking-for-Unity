using System;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a delta structure holding eight unmanaged values, four of type <typeparamref name="T1"/> and four of type <typeparamref name="T2"/>.
    /// Used for efficient network synchronization by only transmitting changed values.
    /// </summary>
    /// <typeparam name="T1">An unmanaged type to be tracked for delta changes (fields a1-d1).</typeparam>
    /// <typeparam name="T2">An unmanaged type to be tracked for delta changes (fields a2-d2).</typeparam>
    [Serializable, Nested]
    [DeclareHorizontalGroup("G1")]
    [DeclareHorizontalGroup("G2")]
    [DeltaSerializable]
    public struct Delta8_t<T1, T2>
       where T1 : unmanaged
       where T2 : unmanaged
    {
        [GroupNext("G1")]
        public T1 a1, b1, c1, d1;

        [GroupNext("G2")]
        public T2 a2, b2, c2, d2;

        public Delta8_t(
            T1 a1, T1 b1, T1 c1, T1 d1,
            T2 a2, T2 b2, T2 c2, T2 d2)
        {
            this.a1 = a1; this.b1 = b1; this.c1 = c1; this.d1 = d1;
            this.a2 = a2; this.b2 = b2; this.c2 = c2; this.d2 = d2;
        }

        public readonly DataBuffer Write(ref Delta8_t<T1, T2> lastDelta)
        {
            return Write(ref lastDelta, out _);
        }

        public readonly DataBuffer Write(ref Delta8_t<T1, T2> lastDelta, out bool changed)
        {
            changed = false;
            var data = NetworkManager.Pool.Rent();
            byte mask = 0;

            if (!a1.Equals(lastDelta.a1)) mask |= 1 << 0;
            if (!b1.Equals(lastDelta.b1)) mask |= 1 << 1;
            if (!c1.Equals(lastDelta.c1)) mask |= 1 << 2;
            if (!d1.Equals(lastDelta.d1)) mask |= 1 << 3;

            if (!a2.Equals(lastDelta.a2)) mask |= 1 << 4;
            if (!b2.Equals(lastDelta.b2)) mask |= 1 << 5;
            if (!c2.Equals(lastDelta.c2)) mask |= 1 << 6;
            if (!d2.Equals(lastDelta.d2)) mask |= 1 << 7;

            data.Write(mask);

            if ((mask & (1 << 0)) != 0) { data.Write(a1); changed = true; }
            if ((mask & (1 << 1)) != 0) { data.Write(b1); changed = true; }
            if ((mask & (1 << 2)) != 0) { data.Write(c1); changed = true; }
            if ((mask & (1 << 3)) != 0) { data.Write(d1); changed = true; }

            if ((mask & (1 << 4)) != 0) { data.Write(a2); changed = true; }
            if ((mask & (1 << 5)) != 0) { data.Write(b2); changed = true; }
            if ((mask & (1 << 6)) != 0) { data.Write(c2); changed = true; }
            if ((mask & (1 << 7)) != 0) { data.Write(d2); changed = true; }

            lastDelta = this;
            return data;
        }

        public static Delta8_t<T1, T2> Read(ref Delta8_t<T1, T2> lastDelta, DataBuffer data)
        {
            Delta8_t<T1, T2> result = lastDelta;
            byte mask = data.Read<byte>();

            if ((mask & (1 << 0)) != 0) result.a1 = data.Read<T1>();
            if ((mask & (1 << 1)) != 0) result.b1 = data.Read<T1>();
            if ((mask & (1 << 2)) != 0) result.c1 = data.Read<T1>();
            if ((mask & (1 << 3)) != 0) result.d1 = data.Read<T1>();

            if ((mask & (1 << 4)) != 0) result.a2 = data.Read<T2>();
            if ((mask & (1 << 5)) != 0) result.b2 = data.Read<T2>();
            if ((mask & (1 << 6)) != 0) result.c2 = data.Read<T2>();
            if ((mask & (1 << 7)) != 0) result.d2 = data.Read<T2>();

            lastDelta = result;
            return result;
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Delta8_t<T1, T2> other)
            {
                return a1.Equals(other.a1) && b1.Equals(other.b1) && c1.Equals(other.c1) && d1.Equals(other.d1) &&
                       a2.Equals(other.a2) && b2.Equals(other.b2) && c2.Equals(other.c2) && d2.Equals(other.d2);
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(a1, b1, c1, d1, a2, b2, c2, d2);
        }

        public override readonly string ToString()
        {
            return $"Delta8_t<{typeof(T1).Name}, {typeof(T2).Name}>(a1: {a1}, b1: {b1}, c1: {c1}, d1: {d1}, a2: {a2}, b2: {b2}, c2: {c2}, d2: {d2})";
        }
    }
}