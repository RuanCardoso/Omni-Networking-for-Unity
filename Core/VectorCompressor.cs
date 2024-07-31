using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/**
 *
 * @author Ismail <ismaiil_0234@hotmail.com>
 *
 */

namespace Omni.Core
{
    /// <summary>
    /// Min/Max Values (X / Y / Z)<br/>
    /// Min Values: -9999.99f / -9999.99f / -9999.99f<br/>
    /// Max Values: 9999.99f / 9999.99f / 9999.99f<br/>
    /// </summary>
    public static class VectorCompressor
    {
        private const long BigNumber = 1000000L * 1000000L * 1000000L;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Compress(Vector3 vector3)
        {
            return Compress(vector3.x, vector3.y, vector3.z);
        }

        public static long Compress(float x, float y, float z)
        {
            unchecked
            {
                var qData = 0;
                if (x < 0)
                {
                    qData |= 0x0000001;
                }

                if (y < 0)
                {
                    qData |= 0x0000002;
                }

                if (z < 0)
                {
                    qData |= 0x0000004;
                }

                var xData = (long)(Math.Abs(x) * 100);
                var yData = (long)(Math.Abs(y) * 100) * 1000000;
                var zData = (long)(Math.Abs(z) * 100) * 1000000 * 1000000;

                return (1000000000000000000 * (long)qData) + xData + yData + zData;
            }
        }

        public static Vector3 Decompress(long longNumber)
        {
            unchecked
            {
                var flag = (byte)(longNumber / BigNumber);
                longNumber -= BigNumber * flag;

                var zData = longNumber / (1000000L * 1000000L);
                longNumber -= 1000000L * 1000000L * zData;

                var yData = longNumber / 1000000L;
                longNumber -= 1000000L * yData;

                if ((flag & 0x0000001) == 0x0000001)
                {
                    longNumber *= -1;
                }

                if ((flag & 0x0000002) == 0x0000002)
                {
                    yData *= -1;
                }

                if ((flag & 0x0000004) == 0x0000004)
                {
                    zData *= -1;
                }

                return new Vector3(longNumber / 100f, yData / 100f, zData / 100f);
            }
        }
    }
}
