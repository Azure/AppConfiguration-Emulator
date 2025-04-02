// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ClaimTypes = Microsoft.AppConfig.Service.Authorization.ClaimTypes;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public class EntraIdCredentialValidator : ICredentialValidator
    {
        private const string LoggerCategory = "Microsoft.AppConfig.Service.Authentication.AzureAd.CredentialValidator";
        private static readonly string UnknownScope = Guid.NewGuid().ToString();

        private readonly IAuthorizationProvider _provider;
        private readonly TenantOptions _tenant;
        private readonly EntraIdOptions _options;
        private readonly ILogger _logger;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configManager;

        public EntraIdCredentialValidator(
            IAuthorizationProvider provider,
            IOptions<TenantOptions> tenant,
            IOptions<EntraIdOptions> options,
            ILoggerFactory logFactory)
        {
            ValidateOptions(options?.Value);

            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));

            _logger = logFactory?.CreateLogger(LoggerCategory) ?? throw new ArgumentNullException(nameof(logFactory));

            //
            // Identity Config manager
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_options.ActiveDirectoryEndpoint}Common/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever())
            {
                AutomaticRefreshInterval = _options.OpenIdMaxStaleInterval,

                RefreshInterval = _options.OpenIdMinRefreshInterval
            };
        }

        public bool CanValidate(string scheme) => scheme == AuthenticationShemes.EntraId;

        public bool CanChallenge() => _tenant.EntraIdAuthenticationEnabled;

        public async ValueTask<CredentialValidationResult> Validate(
            Credential credential,
            CancellationToken cancellationToken)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var result = new CredentialValidationResult();

            //
            // Credential
            ClaimsPrincipal principal = await ValidateCredential(credential, result, cancellationToken);

            if (principal == null)
            {
                return result;
            }

            //
            // RBAC
            RbacResult rbac = await _provider.CheckAccess(principal, Actions.All, cancellationToken);

            if (!ValidateRbac(rbac, result))
            {
                return result;
            }

            principal.AddAuthorizationIdentity(rbac.Decisions);

            principal.AddIdentity(new ClaimsIdentity(credential.Scheme));

            result.Principal = principal;

            result.HasSucceeded = true;

            return result;
        }

        private async Task<ClaimsPrincipal> ValidateCredential(
            Credential credential,
            CredentialValidationResult result,
            CancellationToken cancellationToken)
        {
            Debug.Assert(credential != null);

            if (string.IsNullOrEmpty(credential.Value))
            {
                result.Error = Errors.InvalidToken.Message;

                return null;
            }

            Debug.Assert(credential != null);

            ClaimsPrincipal principal = null;
            SecurityToken token = null;

            var tokenHandler = new JwtSecurityTokenHandler();

            if (string.IsNullOrEmpty(credential.Value) ||
                !tokenHandler.CanReadToken(credential.Value))
            {
                return null;
            }

            try
            {
                try
                {
                    principal = tokenHandler.ValidateToken(
                        credential.Value,
                        await GetTokenValidationParameters(credential, cancellationToken),
                        out token);
                }
                catch (SecurityTokenSignatureKeyNotFoundException)
                {
                    //
                    // Signing keys may need a refresh
                    _configManager.RequestRefresh();

                    //
                    // Try again
                    principal = tokenHandler.ValidateToken(
                        credential.Value,
                        await GetTokenValidationParameters(credential, cancellationToken),
                        out token);
                }
            }
            catch (SecurityTokenException)
            {
                //
                // The token can't be validated
                // Note that the input is untrusted, so logging here is not optimal
            }
            catch (ArgumentException)
            {
                //
                // Hack:
                // There is a bug in ADAL that doesn't handle bad json payload. It manifests with ArgumentException from Json.NET.
                // The caller can't validate the input. CanReadToken also doesn't work.
                // The only option here is to ignore and fail the token validation.
            }

            //
            // Append some JWT claims
            if (token is JwtSecurityToken jwt &&
                jwt.Claims != null &&
                principal != null)
            {
                ClaimsIdentity identity = new ClaimsIdentity(jwt.Claims.Where(c =>
                    c.Type == ClaimTypes.TenantId ||
                    c.Type == ClaimTypes.ObjectId ||
                    c.Type == ClaimTypes.AppId ||
                    c.Type == ClaimTypes.Azp));

                Claim scopes = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Scopes);

                if (scopes != null)
                {
                    AddScopeClaims(identity, scopes);
                }

                principal.AddIdentity(identity);
            }

            return principal;
        }

        private async Task<TokenValidationParameters> GetTokenValidationParameters(
            Credential credential,
            CancellationToken cancellationToken)
        {
            Debug.Assert(credential != null);

            OpenIdConnectConfiguration config = await Retry.Execute(async () =>
            {
                try
                {
                    return await _configManager
                        .GetConfigurationAsync(cancellationToken)
                        .WithLogError(_logger);
                }
                catch (InvalidOperationException)
                {
                    _configManager.RequestRefresh();

                    throw;
                }
            },
            cancellationToken);

            Debug.Assert(config != null);

            return new TokenValidationParameters
            {
                //
                // Issuer
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                IssuerValidator = ValidateIssuerWithTenantId,
                IssuerSigningKeys = config.SigningKeys,

                //
                // Audience
                ValidateAudience = true,
                AudienceValidator = ValidateAudienceIgnoreCase,
                ValidAudiences = _options.GlobalAudiences
            };
        }

        /// <summary>
        /// Scopes the identities permitted actions to a subset of those granted via standard authorization flows.
        /// </summary>
        /// <param name="identity">The identity to scope actions for.</param>
        /// <param name="scp">A scope claim obtained through authentication specifying a subset of permitted actions available to the identity.</param>
        internal void AddScopeClaims(ClaimsIdentity identity, Claim scp)
        {
            Debug.Assert(identity != null);

            Debug.Assert(scp != null);

            string scopes = scp.Value;

            int pos = 0;

            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[pos] == ' ')
                {
                    pos++;

                    continue;
                }

                if (i == scopes.Length - 1 || scopes[i + 1] == ' ')
                {
                    ReadOnlySpan<char> scope = scopes.AsSpan(pos, i - pos + 1);

                    if (scope.Equals(Scopes.KeyValueRead.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.KeyValueRead));
                    }
                    else if (scope.Equals(Scopes.KeyValueWrite.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.KeyValueWrite));
                    }
                    else if (scope.Equals(Scopes.KeyValueDelete.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.KeyValueDelete));
                    }
                    else if (scope.Equals(Scopes.SnapshotRead.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.SnapshotRead));
                    }
                    else if (scope.Equals(Scopes.SnapshotWrite.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.SnapshotCreate));
                    }
                    else if (scope.Equals(Scopes.SnapshotAction.AsSpan(), StringComparison.Ordinal))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.ActionScope, Actions.SnapshotArchive));
                    }

                    pos = i + 1;
                }
            }

            if (!identity.HasClaim(
                c => c.Type == ClaimTypes.ActionScope ||
                c.Type == ClaimTypes.UserImpersonation))
            {
                //
                // Should not occur, but in worst case add a scope that does not map to any action to effectively deny all in this unexpected scenario
                identity.AddClaim(new Claim(ClaimTypes.ActionScope, UnknownScope));
            }
        }

        private static void ValidateOptions(EntraIdOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.ActiveDirectoryEndpoint))
            {
                throw new ArgumentNullException(nameof(options.ActiveDirectoryEndpoint));
            }

            if (options.GlobalAudiences == null)
            {
                throw new ArgumentNullException(nameof(options.GlobalAudiences));
            }

            if (!options.GlobalAudiences.Any())
            {
                throw new ArgumentException(nameof(options.GlobalAudiences));
            }

            if (options.OpenIdMaxStaleInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.OpenIdMaxStaleInterval));
            }

            if (options.OpenIdMinRefreshInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.OpenIdMinRefreshInterval));
            }
        }

        private bool ValidateRbac(RbacResult rbac, CredentialValidationResult result)
        {
            if (rbac.Error != null)
            {
                result.Error = rbac.Error.Message;

                return false;
            }

            return true;
        }

        private static bool ValidateAudienceIgnoreCase(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (audiences == null ||
                validationParameters?.ValidAudiences == null)
            {
                return false;
            }

            foreach (string aud in audiences)
            {
                foreach (string validAud in validationParameters.ValidAudiences)
                {
                    if (validAud.Equals(aud, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ValidateIssuerWithTenantId(string issuer, SecurityToken token, TokenValidationParameters parameters)
        {
            if (token is JwtSecurityToken jwt)
            {
                if (jwt.Payload.TryGetValue("tid", out var value) && value is string tid)
                {
                    if (parameters.ValidIssuer.Replace("{tenantid}", tid) == issuer)
                    {
                        return issuer;
                    }
                }
            }

            throw new SecurityTokenInvalidIssuerException("Issuer validation failed")
            {
                InvalidIssuer = issuer
            };
        }
    }
}
