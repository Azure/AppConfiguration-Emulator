using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    class MediaUtility
    {
        /// <summary>
        /// Generates a blob name for the provided app id and media name.
        /// Azure Storage Blob Name Constraints can be found here:
        /// https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
        /// 
        /// A blob name can contain any combination of characters.
        /// A blob name must be at least one character long and cannot be more than 1,024 characters long, for blobs in Azure Storage.
        /// The Azure Storage emulator supports blob names up to 256 characters long. For more information, see Use the Azure storage emulator for development and testing.
        /// Blob names are case-sensitive.
        /// Reserved URL characters must be properly escaped.
        /// The number of path segments comprising the blob name cannot exceed 254. A path segment is the string between consecutive delimiter characters(e.g., the forward slash '/') that corresponds to the name of a virtual directory.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GenerateBlobName(string appId, string mediaName)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrEmpty(mediaName))
            {
                throw new ArgumentNullException(nameof(mediaName));
            }

            //
            // + 1 to account for the concatenating '/'
            if (appId.Length + mediaName.Length + 1 > 1024)
            {
                throw new ArgumentOutOfRangeException($"{nameof(appId)},{nameof(mediaName)}");
            }

            //
            // App Id formats
            // (old) {appName}.{utcNowSeconds} where app name is alphanumeric + '-' + '_'
            // (new/current) b64url(rand(256))
            foreach (char c in appId)
            {
                if (!(c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9' ||
                    c == '-' ||
                    c == '_' ||
                    c == '.'))
                {
                    throw new ArgumentException(
                        "Unexpected app id format.",
                        nameof(appId));
                }
            }

            //
            // Validate expectations for media name
            foreach (char c in mediaName)
            {
                if (!(c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9' ||
                    c == '-' ||
                    c == '_'))
                {
                    throw new ArgumentException(
                        "Expected base64-URL character set",
                        nameof(mediaName));
                }
            }

            return $"{appId}/{mediaName}";
        }
    }
}
