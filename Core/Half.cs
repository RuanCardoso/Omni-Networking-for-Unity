/// ================ Half.cs ====================
/// The code is free to use for any reason without any restrictions.
/// Ladislav Lang (2009), Joannes Vermorel (2017)

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Omni.Shared;

namespace Omni.Core
{
    public sealed class HalfJsonConverter : JsonConverter<Half>
    {
        public override Half ReadJson(JsonReader reader, Type objectType, Half existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            JsonToken tokenType = reader.TokenType;
            if (tokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot convert null value to Half.");
            }

            switch (tokenType)
            {
                case JsonToken.Float:
                    {
                        float value = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
                        return new Half(value);
                    }

                case JsonToken.Integer:
                    {
                        int value = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        return new Half(value);
                    }

                case JsonToken.String:
                    {
                        string value = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
                        return Half.Parse(value, CultureInfo.InvariantCulture);
                    }
                default:
                    {
                        throw new JsonSerializationException(
                            $"Unexpected token or value when parsing half. Token: {tokenType}, Value: {reader.Value}");
                    }
            }
        }

        public override void WriteJson(JsonWriter writer, Half value, JsonSerializer serializer)
        {
            writer.WriteValue((float)value);
        }
    }

    /// <summary>
    /// Represents a half-precision floating point number.
    /// </summary>
    /// <remarks>
    /// Note:
    ///     Half is not fast enought and precision is also very bad,
    ///     so is should not be used for mathematical computation (use Single instead).
    ///     The main advantage of Half type is lower memory cost: two bytes per number.
    ///     Half is typically used in graphical applications.
    ///
    /// Note:
    ///     All functions, where is used conversion half->float/float->half,
    ///     are approx. ten times slower than float->double/double->float, i.e. ~3ns on 2GHz CPU.
    ///
    /// References:
    ///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
    ///     - Fast Half Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
    ///     - IEEE 754 revision, link: http://grouper.ieee.org/groups/754/
    /// </remarks>
    [Serializable]
    public struct Half : IComparable, IFormattable, IConvertible, IComparable<Half>, IEquatable<Half>
    {
        /// <summary>
        /// Internal representation of the half-precision floating-point number.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ushort Value;

        #region Constants

        /// <summary>
        /// Represents the smallest positive System.Half value greater than zero. This field is constant.
        /// </summary>
        public static readonly Half Epsilon = ToHalf(0x0001);

        /// <summary>
        /// Represents the largest possible value of System.Half. This field is constant.
        /// </summary>
        public static readonly Half MaxValue = ToHalf(0x7bff);

        /// <summary>
        /// Represents the smallest possible value of System.Half. This field is constant.
        /// </summary>
        public static readonly Half MinValue = ToHalf(0xfbff);

        /// <summary>
        /// Represents not a number (NaN). This field is constant.
        /// </summary>
        public static readonly Half NaN = ToHalf(0xfe00);

        /// <summary>
        /// Represents negative infinity. This field is constant.
        /// </summary>
        public static readonly Half NegativeInfinity = ToHalf(0xfc00);

        /// <summary>
        /// Represents positive infinity. This field is constant.
        /// </summary>
        public static readonly Half PositiveInfinity = ToHalf(0x7c00);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified single-precision floating-point number.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(float value)
        {
            this = HalfHelper.SingleToHalf(value);
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified 32-bit signed integer.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(int value)
            : this((float)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified 64-bit signed integer.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(long value)
            : this((float)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified double-precision floating-point number.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(double value)
            : this((float)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified decimal number.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(decimal value)
            : this((float)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(uint value)
            : this((float)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of System.Half to the value of the specified 64-bit unsigned integer.
        /// </summary>
        /// <param name="value">The value to represent as a System.Half.</param>
        public Half(ulong value)
            : this((float)value)
        {
        }

        #endregion

        #region Numeric operators

        /// <summary>
        /// Returns the result of multiplying the specified System.Half value by negative one.
        /// </summary>
        /// <param name="half">A System.Half.</param>
        /// <returns>A System.Half with the value of half, but the opposite sign. -or- Zero, if half is zero.</returns>
        public static Half Negate(Half half)
        {
            return -half;
        }

        /// <summary>
        /// Adds two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>A System.Half value that is the sum of half1 and half2.</returns>
        public static Half Add(Half half1, Half half2)
        {
            return half1 + half2;
        }

        /// <summary>
        /// Subtracts one specified System.Half value from another.
        /// </summary>
        /// <param name="half1">A System.Half (the minuend).</param>
        /// <param name="half2">A System.Half (the subtrahend).</param>
        /// <returns>The System.Half result of subtracting half2 from half1.</returns>
        public static Half Subtract(Half half1, Half half2)
        {
            return half1 - half2;
        }

        /// <summary>
        /// Multiplies two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half (the multiplicand).</param>
        /// <param name="half2">A System.Half (the multiplier).</param>
        /// <returns>A System.Half that is the result of multiplying half1 and half2.</returns>
        public static Half Multiply(Half half1, Half half2)
        {
            return half1 * half2;
        }

        /// <summary>
        /// Divides two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half (the dividend).</param>
        /// <param name="half2">A System.Half (the divisor).</param>
        /// <returns>The System.Half that is the result of dividing half1 by half2.</returns>
        /// <exception cref="System.DivideByZeroException">half2 is zero.</exception>
        public static Half Divide(Half half1, Half half2)
        {
            return half1 / half2;
        }

        /// <summary>
        /// Returns the value of the System.Half operand (the sign of the operand is unchanged).
        /// </summary>
        /// <param name="half">The System.Half operand.</param>
        /// <returns>The value of the operand, half.</returns>
        public static Half operator +(Half half)
        {
            return half;
        }

        /// <summary>
        /// Negates the value of the specified System.Half operand.
        /// </summary>
        /// <param name="half">The System.Half operand.</param>
        /// <returns>The result of half multiplied by negative one (-1).</returns>
        public static Half operator -(Half half)
        {
            return HalfHelper.Negate(half);
        }

        /// <summary>
        /// Increments the System.Half operand by 1.
        /// </summary>
        /// <param name="half">The System.Half operand.</param>
        /// <returns>The value of half incremented by 1.</returns>
        public static Half operator ++(Half half)
        {
            return half + 1f;
        }

        /// <summary>
        /// Decrements the System.Half operand by one.
        /// </summary>
        /// <param name="half">The System.Half operand.</param>
        /// <returns>The value of half decremented by 1.</returns>
        public static Half operator --(Half half)
        {
            return half - 1f;
        }

        /// <summary>
        /// Adds two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>The System.Half result of adding half1 and half2.</returns>
        public static Half operator +(Half half1, Half half2)
        {
            return (Half)((float)half1 + (float)half2);
        }

        /// <summary>
        /// Subtracts two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>The System.Half result of subtracting half1 and half2.</returns>
        public static Half operator -(Half half1, Half half2)
        {
            return (Half)((float)half1 - (float)half2);
        }

        /// <summary>
        /// Multiplies two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>The System.Half result of multiplying half1 by half2.</returns>
        public static Half operator *(Half half1, Half half2)
        {
            return (Half)((float)half1 * (float)half2);
        }

        /// <summary>
        /// Divides two specified System.Half values.
        /// </summary>
        /// <param name="half1">A System.Half (the dividend).</param>
        /// <param name="half2">A System.Half (the divisor).</param>
        /// <returns>The System.Half result of half1 by half2.</returns>
        public static Half operator /(Half half1, Half half2)
        {
            return (Half)((float)half1 / (float)half2);
        }

        /// <summary>
        /// Returns a value indicating whether two instances of System.Half are equal.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 and half2 are equal; otherwise, false.</returns>
        public static bool operator ==(Half half1, Half half2)
        {
            return !IsNaN(half1) && (half1.Value == half2.Value);
        }

        /// <summary>
        /// Returns a value indicating whether two instances of System.Half are not equal.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 and half2 are not equal; otherwise, false.</returns>
        public static bool operator !=(Half half1, Half half2)
        {
            return half1.Value != half2.Value;
        }

        /// <summary>
        /// Returns a value indicating whether a specified System.Half is less than another specified System.Half.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 is less than half1; otherwise, false.</returns>
        public static bool operator <(Half half1, Half half2)
        {
            return (float)half1 < (float)half2;
        }

        /// <summary>
        /// Returns a value indicating whether a specified System.Half is greater than another specified System.Half.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 is greater than half2; otherwise, false.</returns>
        public static bool operator >(Half half1, Half half2)
        {
            return (float)half1 > (float)half2;
        }

        /// <summary>
        /// Returns a value indicating whether a specified System.Half is less than or equal to another specified System.Half.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 is less than or equal to half2; otherwise, false.</returns>
        public static bool operator <=(Half half1, Half half2)
        {
            return (half1 == half2) || (half1 < half2);
        }

        /// <summary>
        /// Returns a value indicating whether a specified System.Half is greater than or equal to another specified System.Half.
        /// </summary>
        /// <param name="half1">A System.Half.</param>
        /// <param name="half2">A System.Half.</param>
        /// <returns>true if half1 is greater than or equal to half2; otherwise, false.</returns>
        public static bool operator >=(Half half1, Half half2)
        {
            return (half1 == half2) || (half1 > half2);
        }

        #endregion

        #region Type casting operators

        /// <summary>
        /// Converts an 8-bit unsigned integer to a System.Half.
        /// </summary>
        /// <param name="value">An 8-bit unsigned integer.</param>
        /// <returns>A System.Half that represents the converted 8-bit unsigned integer.</returns>
        public static implicit operator Half(byte value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 16-bit signed integer to a System.Half.
        /// </summary>
        /// <param name="value">A 16-bit signed integer.</param>
        /// <returns>A System.Half that represents the converted 16-bit signed integer.</returns>
        public static implicit operator Half(short value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a Unicode character to a System.Half.
        /// </summary>
        /// <param name="value">A Unicode character.</param>
        /// <returns>A System.Half that represents the converted Unicode character.</returns>
        public static implicit operator Half(char value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 32-bit signed integer to a System.Half.
        /// </summary>
        /// <param name="value">A 32-bit signed integer.</param>
        /// <returns>A System.Half that represents the converted 32-bit signed integer.</returns>
        public static implicit operator Half(int value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to a System.Half.
        /// </summary>
        /// <param name="value">A 64-bit signed integer.</param>
        /// <returns>A System.Half that represents the converted 64-bit signed integer.</returns>
        public static implicit operator Half(long value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a single-precision floating-point number to a System.Half.
        /// </summary>
        /// <param name="value">A single-precision floating-point number.</param>
        /// <returns>A System.Half that represents the converted single-precision floating point number.</returns>
        public static implicit operator Half(float value)
        {
            return new Half(value);
        }

        /// <summary>
        /// Converts a double-precision floating-point number to a System.Half.
        /// </summary>
        /// <param name="value">A double-precision floating-point number.</param>
        /// <returns>A System.Half that represents the converted double-precision floating point number.</returns>
        public static implicit operator Half(double value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a decimal number to a System.Half.
        /// </summary>
        /// <param name="value">decimal number</param>
        /// <returns>A System.Half that represents the converted decimal number.</returns>
        public static explicit operator Half(decimal value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a System.Half to an 8-bit unsigned integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>An 8-bit unsigned integer that represents the converted System.Half.</returns>
        public static explicit operator byte(Half value)
        {
            return (byte)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a Unicode character.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A Unicode character that represents the converted System.Half.</returns>
        public static explicit operator char(Half value)
        {
            return (char)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 16-bit signed integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 16-bit signed integer that represents the converted System.Half.</returns>
        public static explicit operator short(Half value)
        {
            return (short)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 32-bit signed integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 32-bit signed integer that represents the converted System.Half.</returns>
        public static explicit operator int(Half value)
        {
            return (int)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 64-bit signed integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 64-bit signed integer that represents the converted System.Half.</returns>
        public static explicit operator long(Half value)
        {
            return (long)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a single-precision floating-point number.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A single-precision floating-point number that represents the converted System.Half.</returns>
        public static implicit operator float(Half value)
        {
            return HalfHelper.HalfToSingle(value);
        }

        /// <summary>
        /// Converts a System.Half to a double-precision floating-point number.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A double-precision floating-point number that represents the converted System.Half.</returns>
        public static implicit operator double(Half value)
        {
            return (float)value;
        }

        /// <summary>
        /// Converts a System.Half to a decimal number.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A decimal number that represents the converted System.Half.</returns>
        public static explicit operator decimal(Half value)
        {
            return (decimal)(float)value;
        }

        /// <summary>
        /// Converts an 8-bit signed integer to a System.Half.
        /// </summary>
        /// <param name="value">An 8-bit signed integer.</param>
        /// <returns>A System.Half that represents the converted 8-bit signed integer.</returns>
        public static implicit operator Half(sbyte value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 16-bit unsigned integer to a System.Half.
        /// </summary>
        /// <param name="value">A 16-bit unsigned integer.</param>
        /// <returns>A System.Half that represents the converted 16-bit unsigned integer.</returns>
        public static implicit operator Half(ushort value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 32-bit unsigned integer to a System.Half.
        /// </summary>
        /// <param name="value">A 32-bit unsigned integer.</param>
        /// <returns>A System.Half that represents the converted 32-bit unsigned integer.</returns>
        public static implicit operator Half(uint value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a 64-bit unsigned integer to a System.Half.
        /// </summary>
        /// <param name="value">A 64-bit unsigned integer.</param>
        /// <returns>A System.Half that represents the converted 64-bit unsigned integer.</returns>
        public static implicit operator Half(ulong value)
        {
            return new Half((float)value);
        }

        /// <summary>
        /// Converts a System.Half to an 8-bit signed integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>An 8-bit signed integer that represents the converted System.Half.</returns>
        public static explicit operator sbyte(Half value)
        {
            return (sbyte)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 16-bit unsigned integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 16-bit unsigned integer that represents the converted System.Half.</returns>
        public static explicit operator ushort(Half value)
        {
            return (ushort)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 32-bit unsigned integer that represents the converted System.Half.</returns>
        public static explicit operator uint(Half value)
        {
            return (uint)(float)value;
        }

        /// <summary>
        /// Converts a System.Half to a 64-bit unsigned integer.
        /// </summary>
        /// <param name="value">A System.Half to convert.</param>
        /// <returns>A 64-bit unsigned integer that represents the converted System.Half.</returns>
        public static explicit operator ulong(Half value)
        {
            return (ulong)(float)value;
        }

        #endregion

        /// <summary>
        /// Compares this instance to a specified System.Half object.
        /// </summary>
        /// <param name="other">A System.Half object.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and value.
        /// Return Value Meaning Less than zero This instance is less than value. Zero
        /// This instance is equal to value. Greater than zero This instance is greater than value.
        /// </returns>
        public readonly int CompareTo(Half other)
        {
            int result = 0;
            if (this < other)
            {
                result = -1;
            }
            else if (this > other)
            {
                result = 1;
            }
            else if (this != other)
            {
                if (!IsNaN(this))
                {
                    result = 1;
                }
                else if (!IsNaN(other))
                {
                    result = -1;
                }
            }

            return result;
        }

        /// <summary>
        /// Compares this instance to a specified System.Object.
        /// </summary>
        /// <param name="obj">An System.Object or null.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and value.
        /// Return Value Meaning Less than zero This instance is less than value. Zero
        /// This instance is equal to value. Greater than zero This instance is greater
        /// than value. -or- value is null.
        /// </returns>
        /// <exception cref="System.ArgumentException">value is not a System.Half</exception>
        public readonly int CompareTo(object obj)
        {
            int result = 0;
            if (obj == null)
            {
                result = 1;
            }
            else
            {
                if (obj is Half half)
                {
                    result = CompareTo(half);
                }
                else
                {
                    throw new ArgumentException("Object must be of type Half.");
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a value indicating whether this instance and a specified System.Half object represent the same value.
        /// </summary>
        /// <param name="other">A System.Half object to compare to this instance.</param>
        /// <returns>true if value is equal to this instance; otherwise, false.</returns>
        public readonly bool Equals(Half other)
        {
            return (other == this) || (IsNaN(other) && IsNaN(this));
        }

        /// <summary>
        /// Returns a value indicating whether this instance and a specified System.Object
        /// represent the same type and value.
        /// </summary>
        /// <param name="obj">An System.Object.</param>
        /// <returns>true if value is a System.Half and equal to this instance; otherwise, false.</returns>
        public override readonly bool Equals(object obj)
        {
            bool result = false;
            if (obj is Half half)
            {
                if ((half == this) || (IsNaN(half) && IsNaN(this)))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override readonly int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Returns the System.TypeCode for value type System.Half.
        /// </summary>
        /// <returns>The enumerated constant (TypeCode)255.</returns>
        public readonly TypeCode GetTypeCode()
        {
            return (TypeCode)255;
        }

        #region BitConverter & Math methods for Half

        /// <summary>
        /// Returns the specified half-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static byte[] GetBytes(Half value)
        {
            return BitConverter.GetBytes(value.Value);
        }

        /// <summary>
        /// Converts the value of a specified instance of System.Half to its equivalent binary representation.
        /// </summary>
        /// <param name="value">A System.Half value.</param>
        /// <returns>A 16-bit unsigned integer that contain the binary representation of value.</returns>
        public static ushort GetBits(Half value)
        {
            return value.Value;
        }

        /// <summary>
        /// Returns a half-precision floating point number converted from two bytes
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A half-precision floating point number formed by two bytes beginning at startIndex.</returns>
        /// <exception cref="System.ArgumentException">
        /// startIndex is greater than or equal to the length of value minus 1, and is
        /// less than or equal to the length of value minus 1.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">value is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">startIndex is less than zero or greater than the length of value minus 1.</exception>
        public static Half ToHalf(byte[] value, int startIndex)
        {
            return ToHalf((ushort)BitConverter.ToInt16(value, startIndex));
        }

        /// <summary>
        /// Returns a half-precision floating point number converted from its binary representation.
        /// </summary>
        /// <param name="bits">Binary representation of System.Half value</param>
        /// <returns>A half-precision floating point number formed by its binary representation.</returns>
        public static Half ToHalf(ushort bits)
        {
            return new Half { Value = bits };
        }

        /// <summary>
        /// Returns a value indicating the sign of a half-precision floating-point number.
        /// </summary>
        /// <param name="value">A signed number.</param>
        /// <returns>
        /// A number indicating the sign of value. Number Description -1 value is less
        /// than zero. 0 value is equal to zero. 1 value is greater than zero.
        /// </returns>
        /// <exception cref="System.ArithmeticException">value is equal to System.Half.NaN.</exception>
        public static int Sign(Half value)
        {
            if (value < 0)
            {
                return -1;
            }
            else if (value > 0)
            {
                return 1;
            }
            else
            {
                if (value != 0)
                {
                    throw new ArithmeticException(
                        "Function does not accept floating point Not-a-Number values."
                    );
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the absolute value of a half-precision floating-point number.
        /// </summary>
        /// <param name="value">A number in the range System.Half.MinValue ≤ value ≤ System.Half.MaxValue.</param>
        /// <returns>A half-precision floating-point number, x, such that 0 ≤ x ≤System.Half.MaxValue.</returns>
        public static Half Abs(Half value)
        {
            return HalfHelper.Abs(value);
        }

        /// <summary>
        /// Returns the larger of two half-precision floating-point numbers.
        /// </summary>
        /// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
        /// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
        /// <returns>
        /// Parameter value1 or value2, whichever is larger. If value1, or value2, or both val1
        /// and value2 are equal to System.Half.NaN, System.Half.NaN is returned.
        /// </returns>
        public static Half Max(Half value1, Half value2)
        {
            return (value1 < value2) ? value2 : value1;
        }

        /// <summary>
        /// Returns the smaller of two half-precision floating-point numbers.
        /// </summary>
        /// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
        /// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
        /// <returns>
        /// Parameter value1 or value2, whichever is smaller. If value1, or value2, or both val1
        /// and value2 are equal to System.Half.NaN, System.Half.NaN is returned.
        /// </returns>
        public static Half Min(Half value1, Half value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        #endregion

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to not a number (System.Half.NaN).
        /// </summary>
        /// <param name="half">A half-precision floating-point number.</param>
        /// <returns>true if value evaluates to not a number (System.Half.NaN); otherwise, false.</returns>
        public static bool IsNaN(Half half)
        {
            return HalfHelper.IsNaN(half);
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to negative or positive infinity.
        /// </summary>
        /// <param name="half">A half-precision floating-point number.</param>
        /// <returns>true if half evaluates to System.Half.PositiveInfinity or System.Half.NegativeInfinity; otherwise, false.</returns>
        public static bool IsInfinity(Half half)
        {
            return HalfHelper.IsInfinity(half);
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to negative infinity.
        /// </summary>
        /// <param name="half">A half-precision floating-point number.</param>
        /// <returns>true if half evaluates to System.Half.NegativeInfinity; otherwise, false.</returns>
        public static bool IsNegativeInfinity(Half half)
        {
            return HalfHelper.IsNegativeInfinity(half);
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to positive infinity.
        /// </summary>
        /// <param name="half">A half-precision floating-point number.</param>
        /// <returns>true if half evaluates to System.Half.PositiveInfinity; otherwise, false.</returns>
        public static bool IsPositiveInfinity(Half half)
        {
            return HalfHelper.IsPositiveInfinity(half);
        }

        #region String operations (Parse and ToString)

        /// <summary>
        /// Converts the string representation of a number to its System.Half equivalent.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <returns>The System.Half number equivalent to the number contained in value.</returns>
        /// <exception cref="System.ArgumentNullException">value is null.</exception>
        /// <exception cref="System.FormatException">value is not in the correct format.</exception>
        /// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
        public static Half Parse(string value)
        {
            return (Half)float.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the string representation of a number to its System.Half equivalent
        /// using the specified culture-specific format information.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <param name="provider">An System.IFormatProvider that supplies culture-specific parsing information about value.</param>
        /// <returns>The System.Half number equivalent to the number contained in s as specified by provider.</returns>
        /// <exception cref="System.ArgumentNullException">value is null.</exception>
        /// <exception cref="System.FormatException">value is not in the correct format.</exception>
        /// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
        public static Half Parse(string value, IFormatProvider provider)
        {
            return (Half)float.Parse(value, provider);
        }

        /// <summary>
        /// Converts the string representation of a number in a specified style to its System.Half equivalent.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <param name="style">
        /// A bitwise combination of System.Globalization.NumberStyles values that indicates
        /// the style elements that can be present in value. A typical value to specify is
        /// System.Globalization.NumberStyles.Number.
        /// </param>
        /// <returns>The System.Half number equivalent to the number contained in s as specified by style.</returns>
        /// <exception cref="System.ArgumentNullException">value is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// style is not a System.Globalization.NumberStyles value. -or- style is the
        /// System.Globalization.NumberStyles.AllowHexSpecifier value.
        /// </exception>
        /// <exception cref="System.FormatException">value is not in the correct format.</exception>
        /// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
        public static Half Parse(string value, NumberStyles style)
        {
            return (Half)float.Parse(value, style, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the string representation of a number to its System.Half equivalent
        /// using the specified style and culture-specific format.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <param name="style">
        /// A bitwise combination of System.Globalization.NumberStyles values that indicates
        /// the style elements that can be present in value. A typical value to specify is
        /// System.Globalization.NumberStyles.Number.
        /// </param>
        /// <param name="provider">An System.IFormatProvider object that supplies culture-specific information about the format of value.</param>
        /// <returns>The System.Half number equivalent to the number contained in s as specified by style and provider.</returns>
        /// <exception cref="System.ArgumentNullException">value is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// style is not a System.Globalization.NumberStyles value. -or- style is the
        /// System.Globalization.NumberStyles.AllowHexSpecifier value.
        /// </exception>
        /// <exception cref="System.FormatException">value is not in the correct format.</exception>
        /// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
        public static Half Parse(string value, NumberStyles style, IFormatProvider provider)
        {
            return (Half)float.Parse(value, style, provider);
        }

        /// <summary>
        /// Converts the string representation of a number to its System.Half equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <param name="result">
        /// When this method returns, contains the System.Half number that is equivalent
        /// to the numeric value contained in value, if the conversion succeeded, or is zero
        /// if the conversion failed. The conversion fails if the s parameter is null,
        /// is not a number in a valid format, or represents a number less than System.Half.MinValue
        /// or greater than System.Half.MaxValue. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string value, out Half result)
        {
            if (float.TryParse(value, out float f))
            {
                result = (Half)f;
                return true;
            }

            result = new Half();
            return false;
        }

        /// <summary>
        /// Converts the string representation of a number to its System.Half equivalent
        /// using the specified style and culture-specific format. A return value indicates
        /// whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">The string representation of the number to convert.</param>
        /// <param name="style">
        /// A bitwise combination of System.Globalization.NumberStyles values that indicates
        /// the permitted format of value. A typical value to specify is System.Globalization.NumberStyles.Number.
        /// </param>
        /// <param name="provider">An System.IFormatProvider object that supplies culture-specific parsing information about value.</param>
        /// <param name="result">
        /// When this method returns, contains the System.Half number that is equivalent
        /// to the numeric value contained in value, if the conversion succeeded, or is zero
        /// if the conversion failed. The conversion fails if the s parameter is null,
        /// is not in a format compliant with style, or represents a number less than
        /// System.Half.MinValue or greater than System.Half.MaxValue. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// style is not a System.Globalization.NumberStyles value. -or- style
        /// is the System.Globalization.NumberStyles.AllowHexSpecifier value.
        /// </exception>
        public static bool TryParse(
            string value,
            NumberStyles style,
            IFormatProvider provider,
            out Half result
        )
        {
            bool parseResult = false;
            if (float.TryParse(value, style, provider, out float f))
            {
                result = (Half)f;
                parseResult = true;
            }
            else
            {
                result = new Half();
            }

            return parseResult;
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>A string that represents the value of this instance.</returns>
        public override readonly string ToString()
        {
            return ((float)this).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation
        /// using the specified culture-specific format information.
        /// </summary>
        /// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by provider.</returns>
        public readonly string ToString(IFormatProvider formatProvider)
        {
            return ((float)this).ToString(formatProvider);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified by format.</returns>
        public readonly string ToString(string format)
        {
            return ((float)this).ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation
        /// using the specified format and culture-specific format information.
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by format and provider.</returns>
        /// <exception cref="System.FormatException">format is invalid.</exception>
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            return ((float)this).ToString(format, formatProvider);
        }

        #endregion

        #region IConvertible Members

        readonly float IConvertible.ToSingle(IFormatProvider provider)
        {
            return this;
        }

        readonly TypeCode IConvertible.GetTypeCode()
        {
            return GetTypeCode();
        }

        readonly bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(this);
        }

        readonly byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(this);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Invalid cast from '{0}' to '{1}'.",
                    "Half",
                    "Char"
                )
            );
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Invalid cast from '{0}' to '{1}'.",
                    "Half",
                    "DateTime"
                )
            );
        }

        readonly decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(this);
        }

        readonly double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(this);
        }

        readonly short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(this);
        }

        readonly int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(this);
        }

        readonly long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(this);
        }

        readonly sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(this);
        }

        readonly string IConvertible.ToString(IFormatProvider provider)
        {
            return Convert.ToString(this, CultureInfo.InvariantCulture);
        }

        readonly object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return (((float)this) as IConvertible).ToType(conversionType, provider);
        }

        readonly ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(this);
        }

        readonly uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(this);
        }

        readonly ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(this);
        }

        #endregion
    }
}

/// ================ HalfHelper.cs ====================

namespace Omni.Core
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public uint UIntValue;
        [FieldOffset(0)]
        public float FloatValue;
    }

    /// <summary>
    /// Helper class for Half conversions and some low level operations.
    /// This class is internally used in the Half class.
    /// </summary>
    /// <remarks>
    /// References:
    ///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
    ///     - Fast Half Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
    /// </remarks>
    public static class HalfHelper
    {
        private static UIntFloat fToIntConverter = new() { FloatValue = 0 };
        private static readonly uint[] MantissaTable = GenerateMantissaTable();
        private static readonly uint[] ExponentTable = GenerateExponentTable();
        private static readonly ushort[] OffsetTable = GenerateOffsetTable();
        private static readonly ushort[] BaseTable = GenerateBaseTable();
        private static readonly sbyte[] ShiftTable = GenerateShiftTable();

        // Transforms the subnormal representation to a normalized one.
        private static uint ConvertMantissa(int i)
        {
            uint m = (uint)(i << 13); // Zero pad mantissa bits
            uint e = 0; // Zero exponent

            // While not normalized
            while ((m & 0x00800000) == 0)
            {
                e -= 0x00800000; // Decrement exponent (1<<23)
                m <<= 1; // Shift mantissa
            }

            m &= unchecked((uint)~0x00800000); // Clear leading 1 bit
            e += 0x38800000; // Adjust bias ((127-14)<<23)
            return m | e; // Return combined number
        }

        private static uint[] GenerateMantissaTable()
        {
            uint[] mantissaTable = new uint[2048];
            mantissaTable[0] = 0;
            for (int i = 1; i < 1024; i++)
            {
                mantissaTable[i] = ConvertMantissa(i);
            }

            for (int i = 1024; i < 2048; i++)
            {
                mantissaTable[i] = (uint)(0x38000000 + ((i - 1024) << 13));
            }

            return mantissaTable;
        }

        private static uint[] GenerateExponentTable()
        {
            uint[] exponentTable = new uint[64];
            exponentTable[0] = 0;
            for (int i = 1; i < 31; i++)
            {
                exponentTable[i] = (uint)(i << 23);
            }

            exponentTable[31] = 0x47800000;
            exponentTable[32] = 0x80000000;
            for (int i = 33; i < 63; i++)
            {
                exponentTable[i] = (uint)(0x80000000 + ((i - 32) << 23));
            }

            exponentTable[63] = 0xc7800000;

            return exponentTable;
        }

        private static ushort[] GenerateOffsetTable()
        {
            ushort[] offsetTable = new ushort[64];
            offsetTable[0] = 0;
            for (int i = 1; i < 32; i++)
            {
                offsetTable[i] = 1024;
            }

            offsetTable[32] = 0;
            for (int i = 33; i < 64; i++)
            {
                offsetTable[i] = 1024;
            }

            return offsetTable;
        }

        private static ushort[] GenerateBaseTable()
        {
            ushort[] baseTable = new ushort[512];
            for (int i = 0; i < 256; ++i)
            {
                sbyte e = (sbyte)(127 - i);
                if (e > 24)
                {
                    // Very small numbers map to zero
                    baseTable[i | 0x000] = 0x0000;
                    baseTable[i | 0x100] = 0x8000;
                }
                else if (e > 14)
                {
                    // Small numbers map to denorms
                    baseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
                    baseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
                }
                else if (e >= -15)
                {
                    // Normal numbers just lose precision
                    baseTable[i | 0x000] = (ushort)((15 - e) << 10);
                    baseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
                }
                else if (e > -128)
                {
                    // Large numbers map to Infinity
                    baseTable[i | 0x000] = 0x7c00;
                    baseTable[i | 0x100] = 0xfc00;
                }
                else
                {
                    // Infinity and NaN's stay Infinity and NaN's
                    baseTable[i | 0x000] = 0x7c00;
                    baseTable[i | 0x100] = 0xfc00;
                }
            }

            return baseTable;
        }

        private static sbyte[] GenerateShiftTable()
        {
            sbyte[] shiftTable = new sbyte[512];
            for (int i = 0; i < 256; ++i)
            {
                sbyte e = (sbyte)(127 - i);
                if (e > 24)
                {
                    // Very small numbers map to zero
                    shiftTable[i | 0x000] = 24;
                    shiftTable[i | 0x100] = 24;
                }
                else if (e > 14)
                {
                    // Small numbers map to denorms
                    shiftTable[i | 0x000] = (sbyte)(e - 1);
                    shiftTable[i | 0x100] = (sbyte)(e - 1);
                }
                else if (e >= -15)
                {
                    // Normal numbers just lose precision
                    shiftTable[i | 0x000] = 13;
                    shiftTable[i | 0x100] = 13;
                }
                else if (e > -128)
                {
                    // Large numbers map to Infinity
                    shiftTable[i | 0x000] = 24;
                    shiftTable[i | 0x100] = 24;
                }
                else
                {
                    // Infinity and NaN's stay Infinity and NaN's
                    shiftTable[i | 0x000] = 13;
                    shiftTable[i | 0x100] = 13;
                }
            }

            return shiftTable;
        }

        public static unsafe float HalfToSingle(Half half)
        {
            uint result =
                MantissaTable[OffsetTable[half.Value >> 10] + (half.Value & 0x3ff)]
                + ExponentTable[half.Value >> 10];
            return *(float*)&result;
        }

        public static unsafe Half SingleToHalf(float single)
        {
            uint value = *(uint*)&single;

            ushort result = (ushort)(
                BaseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> ShiftTable[value >> 23])
            );
            return Half.ToHalf(result);
        }

        public static Half Negate(Half half)
        {
            return Half.ToHalf((ushort)(half.Value ^ 0x8000));
        }

        public static Half Abs(Half half)
        {
            return Half.ToHalf((ushort)(half.Value & 0x7fff));
        }

        public static bool IsNaN(Half half)
        {
            return (half.Value & 0x7fff) > 0x7c00;
        }

        public static bool IsInfinity(Half half)
        {
            return (half.Value & 0x7fff) == 0x7c00;
        }

        public static bool IsPositiveInfinity(Half half)
        {
            return half.Value == 0x7c00;
        }

        public static bool IsNegativeInfinity(Half half)
        {
            return half.Value == 0xfc00;
        }

        public static float Decompress(ushort compressedFloat)
        {
            uint result = MantissaTable[OffsetTable[compressedFloat >> 10] + (compressedFloat & 0x3ff)] + ExponentTable[compressedFloat >> 10];
            return UIntToFloat(result);
        }

        public static ushort Compress(float uncompressedFloat)
        {
            uint value = FloatToUInt(uncompressedFloat);
            return (ushort)(BaseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> ShiftTable[value >> 23]));
        }

        private static uint FloatToUInt(float v)
        {
            fToIntConverter.FloatValue = v;
            return fToIntConverter.UIntValue;
        }

        private static float UIntToFloat(uint v)
        {
            fToIntConverter.UIntValue = v;
            return fToIntConverter.FloatValue;
        }
    }
}