using System;
using System.Collections.Generic;
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
        private static readonly IEqualityComparer<T1> T1Comparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> T2Comparer = EqualityComparer<T2>.Default;

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

        public readonly bool Write(ref Delta16_t<T1, T2> lastDelta, DataBuffer finalBlock)
        {
            ushort mask = 0;
            if (!T1Comparer.Equals(a1, lastDelta.a1)) mask |= 1 << 0;
            if (!T1Comparer.Equals(b1, lastDelta.b1)) mask |= 1 << 1;
            if (!T1Comparer.Equals(c1, lastDelta.c1)) mask |= 1 << 2;
            if (!T1Comparer.Equals(d1, lastDelta.d1)) mask |= 1 << 3;
            if (!T1Comparer.Equals(e1, lastDelta.e1)) mask |= 1 << 4;
            if (!T1Comparer.Equals(f1, lastDelta.f1)) mask |= 1 << 5;
            if (!T1Comparer.Equals(g1, lastDelta.g1)) mask |= 1 << 6;
            if (!T1Comparer.Equals(h1, lastDelta.h1)) mask |= 1 << 7;
            if (!T2Comparer.Equals(a2, lastDelta.a2)) mask |= 1 << 8;
            if (!T2Comparer.Equals(b2, lastDelta.b2)) mask |= 1 << 9;
            if (!T2Comparer.Equals(c2, lastDelta.c2)) mask |= 1 << 10;
            if (!T2Comparer.Equals(d2, lastDelta.d2)) mask |= 1 << 11;
            if (!T2Comparer.Equals(e2, lastDelta.e2)) mask |= 1 << 12;
            if (!T2Comparer.Equals(f2, lastDelta.f2)) mask |= 1 << 13;
            if (!T2Comparer.Equals(g2, lastDelta.g2)) mask |= 1 << 14;
            if (!T2Comparer.Equals(h2, lastDelta.h2)) mask |= 1 << 15;

            finalBlock.Write(mask);
            bool shifted = mask != 0;

            if ((mask & (1 << 0)) != 0) finalBlock.Write(a1);
            if ((mask & (1 << 1)) != 0) finalBlock.Write(b1);
            if ((mask & (1 << 2)) != 0) finalBlock.Write(c1);
            if ((mask & (1 << 3)) != 0) finalBlock.Write(d1);
            if ((mask & (1 << 4)) != 0) finalBlock.Write(e1);
            if ((mask & (1 << 5)) != 0) finalBlock.Write(f1);
            if ((mask & (1 << 6)) != 0) finalBlock.Write(g1);
            if ((mask & (1 << 7)) != 0) finalBlock.Write(h1);
            if ((mask & (1 << 8)) != 0) finalBlock.Write(a2);
            if ((mask & (1 << 9)) != 0) finalBlock.Write(b2);
            if ((mask & (1 << 10)) != 0) finalBlock.Write(c2);
            if ((mask & (1 << 11)) != 0) finalBlock.Write(d2);
            if ((mask & (1 << 12)) != 0) finalBlock.Write(e2);
            if ((mask & (1 << 13)) != 0) finalBlock.Write(f2);
            if ((mask & (1 << 14)) != 0) finalBlock.Write(g2);
            if ((mask & (1 << 15)) != 0) finalBlock.Write(h2);

            lastDelta = this;
            return shifted;
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

        public override readonly string ToString()
        {
            return $"Delta16_t<{typeof(T1).Name}, {typeof(T2).Name}>(a1: {a1}, b1: {b1}, c1: {c1}, d1: {d1}, e1: {e1}, f1: {f1}, g1: {g1}, h1: {h1}, a2: {a2}, b2: {b2}, c2: {c2}, d2: {d2}, e2: {e2}, f2: {f2}, g2: {g2}, h2: {h2})";
        }
    }
}