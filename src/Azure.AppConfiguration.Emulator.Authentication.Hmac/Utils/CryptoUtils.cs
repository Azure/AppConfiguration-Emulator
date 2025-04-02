using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AppConfig.Service.Cryptography
{
    //
    // DO NOT CHANGE ANYTHING HERE WITHOUT SECURITY REVIEW
    static class CryptoUtils
    {
        /// <summary>
        /// Compare buffers in consistent time. The purpose is to prevent time-based attacks
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool CryptoEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.IsEmpty)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b.IsEmpty)
            {
                throw new ArgumentNullException(nameof(b));
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            int result = 0;

            unchecked
            {
                int len = a.Length; // Caching matters because the optimization is off here

                for (int i = 0; i < len; i++)
                {
                    result = result | (a[i] - b[i]);
                }
            }

            return result == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool CryptoEquals(byte[] a, byte[] b, int startIndex, int count)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            if (startIndex < 0 ||
                startIndex >= a.Length ||
                startIndex >= b.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (count <= 0 ||
                startIndex + count > a.Length ||
                startIndex + count > b.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int result = 0;

            unchecked
            {
                int len = startIndex + count;

                for (int i = startIndex; i < len; i++)
                {
                    result = result | (a[i] - b[i]);
                }
            }

            return result == 0;
        }
    }
}
