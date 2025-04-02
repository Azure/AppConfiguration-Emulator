
namespace Microsoft.AppConfig.Service.Authorization
{
    public static class Policies
    {
        public const string KeyValueRead = "https://azconfig.io/authorization/policies/keyvalue/read";
        public const string KeyValueWrite = "https://azconfig.io/authorization/policies/keyvalue/write";
        public const string KeyValueDelete = "https://azconfig.io/authorization/policies/keyvalue/delete";

        public const string SnapshotRead = "https://azconfig.io/authorization/policies/snapshot/read";
        public const string SnapshotCreate = "https://azconfig.io/authorization/policies/snapshot/write";
        public const string SnapshotArchive = "https://azconfig.io/authorization/policies/snapshot/archive";
    }
}
