using System.Security.Claims;

namespace Azure.AppConfiguration.Emulator.Authentication
{
    public class CredentialValidationResult
    {
        public bool HasSucceeded { get; set; }

        public string Error { get; set; }

        public ClaimsPrincipal Principal { get; set; }
    }
}
