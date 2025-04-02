namespace Microsoft.AppConfig.Service.Authorization
{
    public static class ClaimTypes
    {
        public const string Groups = "groups"; // JWT claim
        public const string TenantId = "tid"; // JWT claim 
        public const string ObjectId = "oid"; // JWT claim
        public const string Scopes = "scp"; // JWT claim
        public const string ClaimNames = "_claim_names"; // JWT claim
        public const string ClaimSources = "_claim_sources"; // JWT claim
        public const string Upn = "upn"; // JWT claim

        //
        // https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens
        public const string AppId = "appid"; // JWT claim
        public const string Azp = "azp"; // JWT claim
        public const string AppIdAcr = "appidacr"; // JWT claim
        public const string AzpAcr = "azpacr"; // JWT claim

        public const string AllowAction = "http://azconfig.io/claims/action/allow";
        public const string ActionScope = "http://azconfig.io/claims/action/scope";
        public const string UserImpersonation = "http://azconfig.io/claims/userimpersonation";
    }
}
