namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    /// <summary>
    /// Scopes defined within the Azure App Configuration (ID: Microsoft.Azconfig) first party app
    /// These scopes are defined/managed at https://https://firstpartyportal.msidentity.com/
    /// The following scopes may be added to JWT issued by AAD to limit the actions that the token can be used for
    /// </summary>
    static class Scopes
    {
        public const string KeyValueRead = "KeyValue.Read";

        public const string KeyValueWrite = "KeyValue.Write";

        public const string KeyValueDelete = "KeyValue.Delete";

        public const string SnapshotRead = "Snapshot.Read";

        public const string SnapshotWrite = "Snapshot.Write";

        public const string SnapshotAction = "Snapshot.Action";

        //
        // Special legacy scope
        // See https://stackoverflow.microsoft.com/questions/221600
        public const string UserImpersonation = "user_impersonation";
    }
}
