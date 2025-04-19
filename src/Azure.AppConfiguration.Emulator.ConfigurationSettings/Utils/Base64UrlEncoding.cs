// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    /// <summary>
    /// RFC 4648, section 5
    /// https://tools.ietf.org/html/rfc4648#section-5
    /// </summary>
    public static class Base64UrlEncoding
    {
        private static ArrayPool<char> ArrayPool = ArrayPool<char>.Shared;

        public static string Encode(byte[] input)
        {
            return Encode(input, 0, input.Length);
        }

        /// <summary>
        /// Encodes an input using base64url encoding. Use for fixed size input (ex. hash)
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of the input.</returns>
        public static string Encode(byte[] input, int offset, int length)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length == 0)
            {
                return string.Empty;
            }

            char[] output = ArrayPool.Rent(CalcNumBase64Chars(input.Length));

            try
            {
                int outputLen = Convert.ToBase64CharArray(input, offset, length, output, 0);

                //
                // Map special characters: 
                //    '+' -> '-' 
                //    '/' -> '_'
                for (int i = 0; i < outputLen; ++i)
                {
                    char c = output[i];

                    switch (output[i])
                    {
                        case '+':
                            output[i] = '-';
                            break;

                        case '/':
                            output[i] = '_';
                            break;

                        case '=':
                            //
                            // Padding character. Skip and truncate the string.
                            return new string(output, 0, i);
                    }
                }

                return new string(output, 0, outputLen);
            }
            finally
            {
                ArrayPool.Return(output);
            }
        }

        private static int CalcNumBase64Chars(int inputLength)
        {
            int numBlocks = checked(inputLength + 2) / 3;

            return checked(numBlocks * 4);
        }
    }
}
