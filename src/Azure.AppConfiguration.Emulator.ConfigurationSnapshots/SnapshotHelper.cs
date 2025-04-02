using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    static class SnapshotHelper
    {
        public const string SnapshotType = "kv-snapshot";

        private static byte[] Delimiter = Encoding.Unicode.GetBytes("\n");
        private static byte[] Type = Encoding.Unicode.GetBytes(SnapshotType);

        public static string GenerateId(string snapshotName, string resourceId)
        {
            Debug.Assert(!string.IsNullOrEmpty(snapshotName));
            Debug.Assert(!string.IsNullOrEmpty(resourceId));

            Encoding encoding = Encoding.Unicode;

            //
            // IncrementalHash here offers ~15% perf gain (multi-threading) over the classic SHA256.ComputeHash w/ StringBuilder
            using (IncrementalHash alg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                //
                // AppId
                alg.AppendData(encoding.GetBytes(resourceId));
                alg.AppendData(Delimiter);

                //
                // Key
                alg.AppendData(encoding.GetBytes(snapshotName));
                alg.AppendData(Delimiter);

                //
                // Type
                alg.AppendData(Type);

                // 
                // Base64Url encoding is compatible with DocumentDb resource id restrictions
                // The following characters are restricted and cannot be used in the Id property: '/', '\\', '?', '#'
                // Resource id can't exceed more than 255 characters
                // see https://docs.microsoft.com/en-us/rest/api/cosmos-db/create-a-document#body

                return Base64UrlEncoding.Encode(alg.GetHashAndReset());
            }
        }

        public static string GenerateEtag()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
