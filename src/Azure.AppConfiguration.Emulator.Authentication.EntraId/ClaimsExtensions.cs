using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using ClaimTypes = Microsoft.AppConfig.Service.Authorization.ClaimTypes;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public static class ClaimsExtensions
    {
        public static string GetObjectId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ClaimTypes.ObjectId)?.Value;
        }

        public static string GetTenantId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ClaimTypes.TenantId)?.Value;
        }

        public static string GetApplicationId(this IEnumerable<Claim> claims)
        {
            Claim applicationId = claims.FirstOrDefault(c => c.Type == ClaimTypes.AppId) ??
                claims.FirstOrDefault(c => c.Type == ClaimTypes.Azp);

            return applicationId?.Value;
        }

        public static string GetApplicationAcr(this IEnumerable<Claim> claims)
        {
            Claim applicationAcr = claims.FirstOrDefault(c => c.Type == ClaimTypes.AppIdAcr) ??
                claims.FirstOrDefault(c => c.Type == ClaimTypes.AzpAcr);

            return applicationAcr?.Value;
        }

        public static IEnumerable<string> GetGroups(this IEnumerable<Claim> claims)
        {
            IEnumerable<Claim> groups = claims.Where(c => c.Type == ClaimTypes.Groups && !string.IsNullOrEmpty(c.Value));

            return groups.Any() ? groups.Select(c => c.Value) : null;
        }

        public static string GetClaimNames(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ClaimTypes.ClaimNames && !string.IsNullOrEmpty(c.Value))?.Value;
        }

        public static string GetClaimSources(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ClaimTypes.ClaimSources && !string.IsNullOrEmpty(c.Value))?.Value;
        }
    }
}
