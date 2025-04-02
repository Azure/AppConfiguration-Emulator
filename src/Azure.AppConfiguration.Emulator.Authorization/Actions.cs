using System.Collections.Generic;

namespace Microsoft.AppConfig.Service.Authorization
{
    /// <summary>
    /// ARM data actions registered by RP
    /// Don't modify existing values!
    /// </summary>
    public static class Actions
    {
        /// <summary>
        /// Allows read
        /// </summary>
        public const string KeyValueRead = "Microsoft.AppConfiguration/configurationStores/keyValues/read";

        /// <summary>
        /// Allows write
        /// </summary>
        public const string KeyValueWrite = "Microsoft.AppConfiguration/configurationStores/keyValues/write";

        /// <summary>
        /// Allows delete
        /// Delete will return the deleted value, therefore consider proper description of the action to match expectation.
        /// </summary>
        public const string KeyValueDelete = "Microsoft.AppConfiguration/configurationStores/keyValues/delete";

        /// <summary>
        /// Allows read
        /// </summary>
        public const string SnapshotRead = "Microsoft.AppConfiguration/configurationStores/snapshots/read";

        /// <summary>
        /// Allows snapshot creation when combined with key-value read
        /// </summary>
        public const string SnapshotCreate = "Microsoft.AppConfiguration/configurationStores/snapshots/write";

        /// <summary>
        /// Allows archival + unarchival
        /// </summary>
        public const string SnapshotArchive = "Microsoft.AppConfiguration/configurationStores/snapshots/archive/action";

        /// <summary>
        /// Allows the usage of SAS token
        /// </summary>
        public const string UseSasAuth = "Microsoft.AppConfiguration/configurationStores/useSasAuth/action";

        public static readonly IEnumerable<string> All = new string[]
        {
            KeyValueRead,
            KeyValueWrite,
            KeyValueDelete,
            SnapshotRead,
            SnapshotCreate,
            SnapshotArchive,
            UseSasAuth
        };
    }
}
