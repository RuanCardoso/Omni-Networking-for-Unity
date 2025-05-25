using System;
using Omni.Inspector;

namespace Omni.Core
{
    /// <summary>
    /// Represents a delta structure holding sixteen unmanaged values, eight of type <typeparamref name="T1"/> and eight of type <typeparamref name="T2"/>.
    /// Used for efficient network synchronization by only transmitting changed values.
    /// </summary>
    /// <typeparam name="T1">An unmanaged type to be tracked for delta changes (fields a1-h1).</typeparam>
    /// <typeparam name="T2">An unmanaged type to be tracked for delta changes (fields a2-h2).</typeparam>
    [Serializable, Nested]
    [DeclareHorizontalGroup("G1")]
    [DeclareHorizontalGroup("G2")]
    [DeclareHorizontalGroup("G3")]
    [DeclareHorizontalGroup("G4")]
    [DeltaSerializable]
    public struct Delta16_t<T1, T2>
       where T1 : unmanaged
       where T2 : unmanaged
    {
        [GroupNext("G1")]
        public T1 a1, b1, c1, d1;
        [GroupNext("G2")]
        public T1 e1, f1, g1, h1;

        [GroupNext("G3")]
        public T2 a2, b2, c2, d2;

        [GroupNext("G4")]
        public T2 e2, f2, g2, h2;

        public Delta16_t(
            T1 a1, T1 b1, T1 c1, T1 d1, T1 e1, T1 f1, T1 g1, T1 h1,
            T2 a2, T2 b2, T2 c2, T2 d2, T2 e2, T2 f2, T2 g2, T2 h2)
        {
            this.a1 = a1; this.b1 = b1; this.c1 = c1; this.d1 = d1;
            this.e1 = e1; this.f1 = f1; this.g1 = g1; this.h1 = h1;
            this.a2 = a2; this.b2 = b2; this.c2 = c2; this.d2 = d2;
            this.e2 = e2; this.f2 = f2; this.g2 = g2; this.h2 = h2;
        }

        public readonly DataBuffer Write(ref Delta16_t<T1, T2> lastDelta)
        {
            return Write(ref lastDelta, out _);
        }

        public readonly DataBuffer Write(ref Delta16_t<T1, T2> lastDelta, out bool changed)
        {
            changed = false;
            var data = NetworkManager.Pool.Rent();
            ushort mask = 0;

            if (!a1.Equals(lastDelta.a1)) mask |= 1 << 0;
            if (!b1.Equals(lastDelta.b1)) mask |= 1 << 1;
            if (!c1.Equals(lastDelta.c1)) mask |= 1 << 2;
            if (!d1.Equals(lastDelta.d1)) mask |= 1 << 3;
            if (!e1.Equals(lastDelta.e1)) mask |= 1 << 4;
            if (!f1.Equals(lastDelta.f1)) mask |= 1 << 5;
            if (!g1.Equals(lastDelta.g1)) mask |= 1 << 6;
            if (!h1.Equals(lastDelta.h1)) mask |= 1 << 7;

            if (!a2.Equals(lastDelta.a2)) mask |= 1 << 8;
            if (!b2.Equals(lastDelta.b2)) mask |= 1 << 9;
            if (!c2.Equals(lastDelta.c2)) mask |= 1 << 10;
            if (!d2.Equals(lastDelta.d2)) mask |= 1 << 11;
            if (!e2.Equals(lastDelta.e2)) mask |= 1 << 12;
            if (!f2.Equals(lastDelta.f2)) mask |= 1 << 13;
            if (!g2.Equals(lastDelta.g2)) mask |= 1 << 14;
            if (!h2.Equals(lastDelta.h2)) mask |= 1 << 15;

            data.Write(mask);

            if ((mask & (1 << 0)) != 0) { data.Write(a1); changed = true; }
            if ((mask & (1 << 1)) != 0) { data.Write(b1); changed = true; }
            if ((mask & (1 << 2)) != 0) { data.Write(c1); changed = true; }
            if ((mask & (1 << 3)) != 0) { data.Write(d1); changed = true; }
            if ((mask & (1 << 4)) != 0) { data.Write(e1); changed = true; }
            if ((mask & (1 << 5)) != 0) { data.Write(f1); changed = true; }
            if ((mask & (1 << 6)) != 0) { data.Write(g1); changed = true; }
            if ((mask & (1 << 7)) != 0) { data.Write(h1); changed = true; }

            if ((mask & (1 << 8)) != 0) { data.Write(a2); changed = true; }
            if ((mask & (1 << 9)) != 0) { data.Write(b2); changed = true; }
            if ((mask & (1 << 10)) != 0) { data.Write(c2); changed = true; }
            if ((mask & (1 << 11)) != 0) { data.Write(d2); changed = true; }
            if ((mask & (1 << 12)) != 0) { data.Write(e2); changed = true; }
            if ((mask & (1 << 13)) != 0) { data.Write(f2); changed = true; }
            if ((mask & (1 << 14)) != 0) { data.Write(g2); changed = true; }
            if ((mask & (1 << 15)) != 0) { data.Write(h2); changed = true; }

            lastDelta = this;
            return data;
        }

        public static Delta16_t<T1, T2> Read(ref Delta16_t<T1, T2> lastDelta, DataBuffer data)
        {
            Delta16_t<T1, T2> result = lastDelta;
            ushort mask = data.Read<ushort>();

            if ((mask & (1 << 0)) != 0) result.a1 = data.Read<T1>();
            if ((mask & (1 << 1)) != 0) result.b1 = data.Read<T1>();
            if ((mask & (1 << 2)) != 0) result.c1 = data.Read<T1>();
            if ((mask & (1 << 3)) != 0) result.d1 = data.Read<T1>();
            if ((mask & (1 << 4)) != 0) result.e1 = data.Read<T1>();
            if ((mask & (1 << 5)) != 0) result.f1 = data.Read<T1>();
            if ((mask & (1 << 6)) != 0) result.g1 = data.Read<T1>();
            if ((mask & (1 << 7)) != 0) result.h1 = data.Read<T1>();

            if ((mask & (1 << 8)) != 0) result.a2 = data.Read<T2>();
            if ((mask & (1 << 9)) != 0) result.b2 = data.Read<T2>();
            if ((mask & (1 << 10)) != 0) result.c2 = data.Read<T2>();
            if ((mask & (1 << 11)) != 0) result.d2 = data.Read<T2>();
            if ((mask & (1 << 12)) != 0) result.e2 = data.Read<T2>();
            if ((mask & (1 << 13)) != 0) result.f2 = data.Read<T2>();
            if ((mask & (1 << 14)) != 0) result.g2 = data.Read<T2>();
            if ((mask & (1 << 15)) != 0) result.h2 = data.Read<T2>();

            lastDelta = result;
            return result;
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Delta16_t<T1, T2> other)
            {
                return a1.Equals(other.a1) && b1.Equals(other.b1) && c1.Equals(other.c1) && d1.Equals(other.d1) &&
                       e1.Equals(other.e1) && f1.Equals(other.f1) && g1.Equals(other.g1) && h1.Equals(other.h1) &&
                       a2.Equals(other.a2) && b2.Equals(other.b2) && c2.Equals(other.c2) && d2.Equals(other.d2) &&
                       e2.Equals(other.e2) && f2.Equals(other.f2) && g2.Equals(other.g2) && h2.Equals(other.h2);
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            int hash1 = HashCode.Combine(a1, b1, c1, d1, e1, f1, g1, h1);
            int hash2 = HashCode.Combine(a2, b2, c2, d2, e2, f2, g2, h2);
            return hash1 ^ hash2;
        }

        public override readonly string ToString()
        {
            return $"Delta16_t<{typeof(T1).Name}, {typeof(T2).Name}>(a1: {a1}, b1: {b1}, c1: {c1}, d1: {d1}, e1: {e1}, f1: {f1}, g1: {g1}, h1: {h1}, a2: {a2}, b2: {b2}, c2: {c2}, d2: {d2}, e2: {e2}, f2: {f2}, g2: {g2}, h2: {h2})";
        }
    }
}