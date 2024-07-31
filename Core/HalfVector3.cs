using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Represents a vector with three components, using the Half-precision (16-bit) floating point format.
    /// Optimized for bandwidth efficiency due to the reduced size of Half compared to standard floating point types.
    /// </summary>
    public struct HalfVector3 : IEquatable<HalfVector3>
    {
        public Half x;
        public Half y;
        public Half z;

        public HalfVector3(float x, float y, float z)
        {
            this.x = new Half(x);
            this.y = new Half(y);
            this.z = new Half(z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfVector3(Vector3 vector)
        {
            return new HalfVector3(vector.x, vector.y, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(HalfVector3 vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator +(HalfVector3 a, HalfVector3 b)
        {
            return new HalfVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator -(HalfVector3 a, HalfVector3 b)
        {
            return new HalfVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator -(HalfVector3 a)
        {
            return new HalfVector3(0f - a.x, 0f - a.y, 0f - a.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator *(HalfVector3 a, float d)
        {
            return new HalfVector3(a.x * d, a.y * d, a.z * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator *(float d, HalfVector3 a)
        {
            return new HalfVector3(a.x * d, a.y * d, a.z * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector3 operator /(HalfVector3 a, float d)
        {
            return new HalfVector3(a.x / d, a.y / d, a.z / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HalfVector3 lhs, HalfVector3 rhs)
        {
            float num = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            float num4 = num * num + num2 * num2 + num3 * num3;
            return num4 < 9.9999994E-11f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HalfVector3 lhs, HalfVector3 rhs)
        {
            return !(lhs == rhs);
        }

        public override readonly bool Equals(object obj)
        {
            HalfVector3 other = (HalfVector3)obj;
            return x == other.x && y == other.y && z == other.z;
        }

        public readonly bool Equals(HalfVector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public override readonly string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}, {2:F2})", x, y, z);
        }

        public readonly string ToString(string format)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                $"({{0:{format}}}, {{1:{format}}}, {{2:{format}}})",
                x,
                y,
                z
            );
        }
    }
}
