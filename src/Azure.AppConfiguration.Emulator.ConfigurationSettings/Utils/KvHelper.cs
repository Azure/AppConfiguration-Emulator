// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public static class KvHelper
    {
        private static readonly Encoding Encoding = Encoding.Unicode;
        private static byte[] Delimiter = Encoding.GetBytes("\n");
        private static byte[] Type = Encoding.GetBytes("kvset\n");

        private const int EtagSize = 16; // 128 bits

        //
        // IMPORTANT:
        // Protocol data. Must be stable. Don't change without proper consideration!
        public static string ComputeEtag(Page<KeyValue> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            using IncrementalHash alg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            //
            // Moniker to identify kvset etag
            alg.AppendData(Type);

            //
            // Empty page should have a valid etag
            if (!items.Any())
            {
                alg.AppendData(Delimiter);
            }

            foreach (KeyValue kv in items)
            {
                alg.AppendData(Encoding.GetBytes(kv.Etag ?? string.Empty));
                alg.AppendData(Delimiter);
            }

            //
            // ContinuationToken is part of etag to keep track of any new page added
            if (!string.IsNullOrEmpty(items.ContinuationToken))
            {
                alg.AppendData(Encoding.GetBytes(items.ContinuationToken));
            }

            return Base64UrlEncoding.Encode(alg.GetHashAndReset());
        }

        public static bool CheckPrecondition(KeyValue kv, EtagMatch match, string etag)
        {
            if (kv?.Deleted != null)
            {
                kv = null;
            }

            //
            // etag match
            switch (match)
            {
                case EtagMatch.Match:
                    if (kv == null || (etag != null && etag != kv.Etag))
                    {
                        return false;
                    }

                    break;

                case EtagMatch.NoneMatch:
                    if (kv != null && (etag == null || etag == kv.Etag))
                    {
                        return false;
                    }

                    break;

                default:
                    break;
            }

            return true;
        }

        public static string GenerateEtag()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(EtagSize);

            try
            {
                RandomNumberGenerator.Fill(buffer.AsSpan(0, EtagSize));

                return Base64UrlEncoding.Encode(buffer, 0, EtagSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
