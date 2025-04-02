using Azure.AppConfiguration.Emulator.Authentication;
using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AppConfig.Service.Authentication.Anonymous
{
    class AnonymousCredentialValidator : ICredentialValidator
    {
        private readonly TenantOptions _tenant;
        private readonly AnonymousOptions _options;

        public AnonymousCredentialValidator(
            IOptions<TenantOptions> tenant,
            IOptions<AnonymousOptions> options)
        {
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public bool CanValidate(string scheme) => scheme == AuthenticationSchemes.Anonymous;

        public bool CanChallenge() => _tenant.AnonymousAuthEnabled;

        public ValueTask<CredentialValidationResult> Validate(
            Credential credential,
            CancellationToken cancellationToken)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var result = new CredentialValidationResult();

            //
            // Tenant
            if (!ValidateContext(result))
            {
                return ValueTask.FromResult(result);
            }

            //
            // Credential
            if (!string.IsNullOrEmpty(credential.Value))
            {
                result.Error = Errors.InvalidCredential;

                return ValueTask.FromResult(result);
            }

            //
            // Establish principal
            result.Principal = CreatePrincipal();

            result.HasSucceeded = true;

            return ValueTask.FromResult(result);
        }

        private bool ValidateContext(CredentialValidationResult result)
        {
            if (!_tenant.AnonymousAuthEnabled)
            {
                result.Error = Errors.SchemeNotAllowed;

                return false;
            }

            return true;
        }

        private ClaimsPrincipal CreatePrincipal()
        {
            //
            // Setup claims
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(_options.AnonymousUserSid))
            {
                claims.Add(new Claim(ClaimTypes.Sid, _options.AnonymousUserSid));

            }

            if (!string.IsNullOrEmpty(_options.AnonymousUserRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, _options.AnonymousUserRole));
            }

            //
            // Create principal
            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
                    "Anonymous"));
        }
    }
}
