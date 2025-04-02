// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    static class MediaTypeHeaderValues
    {
        public const string KeyValueApplication = "application/vnd.microsoft.appconfig.kv+json";
        public const string KvsApplication = "application/vnd.microsoft.appconfig.kvset+json";
        public const string SnapshotApplication = "application/vnd.microsoft.appconfig.snapshot+json";
        public const string SnapshotsApplication = "application/vnd.microsoft.appconfig.snapshotset+json";
        public const string KeysApplication = "application/vnd.microsoft.appconfig.keyset+json";
        public const string LabelsApplication = "application/vnd.microsoft.appconfig.labelset+json";
        public const string ProblemApplication = "application/problem+json";
    }
}
