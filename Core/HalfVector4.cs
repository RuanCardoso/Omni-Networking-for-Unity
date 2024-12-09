using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Represents a vector with four components, using the Half-precision (16-bit) floating point format.
    /// Optimized for bandwidth efficiency due to the reduced size of Half compared to standard floating point types.
    /// </summary>
    public struct HalfVector4 : IEquatable<HalfVector4>
    {
        const float kEpsilon = 0.00001F;

        public Half x;
        public Half y;
        public Half z;
        public Half w;

        public HalfVector4(float x, float y, float z, float w)
        {
            this.x = new Half(x);
            this.y = new Half(y);
            this.z = new Half(z);
            this.w = new Half(w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfVector4(Vector4 vector)
        {
            return new HalfVector4(vector.x, vector.y, vector.z, vector.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4(HalfVector4 vector)
        {
            return new Vector4(vector.x, vector.y, vector.z, vector.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator +(HalfVector4 a, HalfVector4 b)
        {
            return new HalfVector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator -(HalfVector4 a, HalfVector4 b)
        {
            return new HalfVector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator -(HalfVector4 a)
        {
            return new HalfVector4(0f - a.x, 0f - a.y, 0f - a.z, 0f - a.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator *(HalfVector4 a, float d)
        {
            return new HalfVector4(a.x * d, a.y * d, a.z * d, a.w * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator *(float d, HalfVector4 a)
        {
            return new HalfVector4(a.x * d, a.y * d, a.z * d, a.w * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector4 operator /(HalfVector4 a, float d)
        {
            return new HalfVector4(a.x / d, a.y / d, a.z / d, a.w / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HalfVector4 lhs, HalfVector4 rhs)
        {
            // Returns false in the presence of NaN values.
            float diffx = lhs.x - rhs.x;
            float diffy = lhs.y - rhs.y;
            float diffz = lhs.z - rhs.z;
            float diffw = lhs.w - rhs.w;
            float sqrmag = diffx * diffx + diffy * diffy + diffz * diffz + diffw * diffw;
            return sqrmag < kEpsilon * kEpsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HalfVector4 lhs, HalfVector4 rhs)
        {
            return !(lhs == rhs);
        }

        public override readonly bool Equals(object obj)
        {
            HalfVector4 other = (HalfVector4)obj;
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        public readonly bool Equals(HalfVector4 other)
        {
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(x, y, z, w);
        }

        public override readonly string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}, {2:F2}, {3:F2})", x, y, z, w);
        }

        public readonly string ToString(string format)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                $"({{0:{format}}}, {{1:{format}}}, {{2:{format}}}, {{3:{format}}})",
                x,
                y,
                z,
                w
            );
        }
    }
}