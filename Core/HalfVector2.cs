using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Represents a vector with two components, using the Half-precision (16-bit) floating point format.
    /// Optimized for bandwidth efficiency due to the reduced size of Half compared to standard floating point types.
    /// </summary>
    public struct HalfVector2 : IEquatable<HalfVector2>
    {
        public Half x;
        public Half y;

        public HalfVector2(float x, float y)
        {
            this.x = new Half(x);
            this.y = new Half(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfVector2(Vector2 vector)
        {
            return new HalfVector2(vector.x, vector.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(HalfVector2 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator +(HalfVector2 a, HalfVector2 b)
        {
            return new HalfVector2(a.x + b.x, a.y + b.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator -(HalfVector2 a, HalfVector2 b)
        {
            return new HalfVector2(a.x - b.x, a.y - b.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator *(HalfVector2 a, HalfVector2 b)
        {
            return new HalfVector2(a.x * b.x, a.y * b.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator /(HalfVector2 a, HalfVector2 b)
        {
            return new HalfVector2(a.x / b.x, a.y / b.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator -(HalfVector2 a)
        {
            return new HalfVector2(0f - a.x, 0f - a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator *(HalfVector2 a, float d)
        {
            return new HalfVector2(a.x * d, a.y * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator *(float d, HalfVector2 a)
        {
            return new HalfVector2(a.x * d, a.y * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfVector2 operator /(HalfVector2 a, float d)
        {
            return new HalfVector2(a.x / d, a.y / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HalfVector2 lhs, HalfVector2 rhs)
        {
            float num = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            return num * num + num2 * num2 < 9.9999994E-11f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HalfVector2 lhs, HalfVector2 rhs)
        {
            return !(lhs == rhs);
        }

        public override readonly bool Equals(object obj)
        {
            HalfVector2 other = (HalfVector2)obj;
            return x == other.x && y == other.y;
        }

        public readonly bool Equals(HalfVector2 other)
        {
            return x == other.x && y == other.y;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public override readonly string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2})", x, y);
        }

        public readonly string ToString(string format)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                $"({{0:{format}}}, {{1:{format}}})",
                x,
                y
            );
        }
    }
}
