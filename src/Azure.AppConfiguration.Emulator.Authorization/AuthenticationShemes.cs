
namespace Microsoft.AppConfig.Service.Authorization
{
    public static class AuthenticationShemes
    {
        public const string HmacSha256 = "HMAC-SHA256";
        public const string EntraId = "Bearer";
        public const string Anonymous = "";

        public static readonly string[] All =
        {
            HmacSha256,
            EntraId,
            Anonymous
        };
    }
}
