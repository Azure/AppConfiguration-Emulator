
namespace Microsoft.AppConfig.Service.Authentication
{
    static class Errors
    {
        public const string InvalidAccessTokenDate = "Invalid access token date";
        public const string AccessTokenExpired = "The access token has expired";

        public const string InvalidCredential = "Invalid Credential";
        public const string DisabledScheme = "Authentication scheme disabled";

        public const string SignedHeadersNotFound = "SignedHeaders parameter is required";
        public const string InvalidSignedHeaders = "Invalid SignedHeaders parameter";
        public const string SignHeaderNotFound = "Required signing request header '{0}' not found";

        public const string SignatureNotFound = "Signature is required";
        public const string InvalidSignature = "Invalid Signature";

        public const string InvalidContentHash = "Invalid request content hash";
    }
}
