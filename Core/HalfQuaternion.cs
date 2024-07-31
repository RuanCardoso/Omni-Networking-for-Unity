using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Represents a quaternion with four components, using the Half-precision (16-bit) floating point format.
    /// Optimized for bandwidth efficiency due to the reduced size of Half compared to standard floating point types.
    /// </summary>
    public struct HalfQuaternion : IEquatable<HalfQuaternion>
    {
        public Half x;
        public Half y;
        public Half z;
        public Half w;

        public HalfQuaternion(float x, float y, float z, float w)
        {
            this.x = new Half(x);
            this.y = new Half(y);
            this.z = new Half(z);
            this.w = new Half(w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfQuaternion(Quaternion quat)
        {
            return new HalfQuaternion(quat.x, quat.y, quat.z, quat.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quaternion(HalfQuaternion quat)
        {
            return new Quaternion(quat.x, quat.y, quat.z, quat.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HalfQuaternion operator *(HalfQuaternion lhs, HalfQuaternion rhs)
        {
            return new HalfQuaternion(
                lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x,
                lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z
            );
        }

        public static Vector3 operator *(HalfQuaternion rotation, Vector3 point)
        {
            float num = rotation.x * 2f;
            float num2 = rotation.y * 2f;
            float num3 = rotation.z * 2f;
            float num4 = rotation.x * num;
            float num5 = rotation.y * num2;
            float num6 = rotation.z * num3;
            float num7 = rotation.x * num2;
            float num8 = rotation.x * num3;
            float num9 = rotation.y * num3;
            float num10 = rotation.w * num;
            float num11 = rotation.w * num2;
            float num12 = rotation.w * num3;
            Vector3 result = default;
            result.x =
                (1f - (num5 + num6)) * point.x
                + (num7 - num12) * point.y
                + (num8 + num11) * point.z;
            result.y =
                (num7 + num12) * point.x
                + (1f - (num4 + num6)) * point.y
                + (num9 - num10) * point.z;
            result.z =
                (num8 - num11) * point.x
                + (num9 + num10) * point.y
                + (1f - (num4 + num5)) * point.z;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqualUsingDot(float dot)
        {
            return dot > 0.999999f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(HalfQuaternion a, HalfQuaternion b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HalfQuaternion lhs, HalfQuaternion rhs)
        {
            return IsEqualUsingDot(Dot(lhs, rhs));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HalfQuaternion lhs, HalfQuaternion rhs)
        {
            return !(lhs == rhs);
        }

        public override readonly bool Equals(object obj)
        {
            HalfQuaternion other = (HalfQuaternion)obj;
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        public readonly bool Equals(HalfQuaternion other)
        {
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(x, y, z, w);
        }

        public override readonly string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:F2}, {1:F2}, {2:F2}, {3:F2})",
                x,
                y,
                z,
                w
            );
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
