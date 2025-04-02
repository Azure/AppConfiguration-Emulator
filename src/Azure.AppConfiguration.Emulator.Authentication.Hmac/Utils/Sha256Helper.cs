// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AppConfig.Service.Security
{
    static class Sha256Helper
    {
        public static readonly byte[] NullHash = CalculateHash(new byte[0]);

        private const int DefaultBufferSize = 2 * 1024; // 2KB

        public static byte[] CalculateHash(byte[] data)
        {
            using (var alg = SHA256.Create())
            {
                return alg.ComputeHash(data);
            }
        }

        public static async Task<byte[]> CalculateHash(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var alg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                byte[] buffer = Pool.Rent(DefaultBufferSize);

                int bytesRead;

                try
                {
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        alg.AppendData(buffer, 0, bytesRead);
                    }

                    return alg.GetHashAndReset();
                }
                finally
                {
                    Pool.Return(buffer);
                }
            }
        }

        public static byte[] CalculateHMAC(byte[] data, byte[] key)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        private static ArrayPool<byte> Pool => ArrayPool<byte>.Shared;
    }
}
