using System;
using System.Collections.Generic;
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
        private static readonly IEqualityComparer<T1> T1Comparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> T2Comparer = EqualityComparer<T2>.Default;

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

        public readonly bool Write(ref Delta8_t<T1, T2> lastDelta, DataBuffer finalBlock)
        {
            byte mask = 0;
            if (!T1Comparer.Equals(a1, lastDelta.a1)) mask |= 1 << 0;
            if (!T1Comparer.Equals(b1, lastDelta.b1)) mask |= 1 << 1;
            if (!T1Comparer.Equals(c1, lastDelta.c1)) mask |= 1 << 2;
            if (!T1Comparer.Equals(d1, lastDelta.d1)) mask |= 1 << 3;
            if (!T2Comparer.Equals(a2, lastDelta.a2)) mask |= 1 << 4;
            if (!T2Comparer.Equals(b2, lastDelta.b2)) mask |= 1 << 5;
            if (!T2Comparer.Equals(c2, lastDelta.c2)) mask |= 1 << 6;
            if (!T2Comparer.Equals(d2, lastDelta.d2)) mask |= 1 << 7;

            finalBlock.Write(mask);
            bool shifted = mask != 0;

            if ((mask & (1 << 0)) != 0) finalBlock.Write(a1);
            if ((mask & (1 << 1)) != 0) finalBlock.Write(b1);
            if ((mask & (1 << 2)) != 0) finalBlock.Write(c1);
            if ((mask & (1 << 3)) != 0) finalBlock.Write(d1);
            if ((mask & (1 << 4)) != 0) finalBlock.Write(a2);
            if ((mask & (1 << 5)) != 0) finalBlock.Write(b2);
            if ((mask & (1 << 6)) != 0) finalBlock.Write(c2);
            if ((mask & (1 << 7)) != 0) finalBlock.Write(d2);

            lastDelta = this;
            return shifted;
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

        public override readonly string ToString()
        {
            return $"Delta8_t<{typeof(T1).Name}, {typeof(T2).Name}>(a1: {a1}, b1: {b1}, c1: {c1}, d1: {d1}, a2: {a2}, b2: {b2}, c2: {c2}, d2: {d2})";
        }
    }
}