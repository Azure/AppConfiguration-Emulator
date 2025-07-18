// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Authentication;
using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.AppConfig.Service.Cryptography;
using Microsoft.AppConfig.Service.Security;
using Microsoft.AppConfig.Service.Security.Authentication.Hmac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AppConfig.Service.Authentication.Hmac
{
    class HmacSha256CredentialValidator : ICredentialValidator
    {
        public static readonly string NullHash = Convert.ToBase64String(Sha256Helper.NullHash);

        private readonly TenantOptions _tenant;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HmacOptions _options;

        public HmacSha256CredentialValidator(
            IOptions<TenantOptions> tenant,
            IHttpContextAccessor httpContextAccessor,
            IOptions<HmacOptions> options)
        {
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public bool CanValidate(string scheme) => scheme == AuthenticationShemes.HmacSha256;

        public bool CanChallenge() => true;

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
            // Tenant
            if (!ValidateContext(result))
            {
                return result;
            }

            //
            // Credential
            if (credential.Value == null)
            {
                result.Error = Errors.InvalidCredential;

                return result;
            }

            HmacToken hmacToken = HmacTokenParser.Parse(credential.Value);

            HttpContext httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                result.Error = Errors.InvalidCredential;

                return result;
            }

            HttpRequest request = httpContext.Request;

            //
            // SignedHeaders
            if (!ValidateSignedHeaders(hmacToken, request.Headers, result))
            {
                return result;
            }

            //
            // TTL
            if (!ValidateTTL(hmacToken, request.Headers, result))
            {
                return result;
            }

            //
            // Credential
            AccessKey signKey = ValidateCredential(hmacToken, result);

            if (signKey == null)
            {
                return result;
            }

            //
            // Signature
            if (!ValidateSignature(
                hmacToken,
                signKey,
                request,
                result))
            {
                return result;
            }

            //
            // Content
            string contentHash = request.ContentLength > 0
                ? await ComputeContentSha256(request.Body, cancellationToken)
                : NullHash;  // A shortcut for an empty payload

            if (!ValidateContent(contentHash, request.Headers, result))
            {
                return result;
            }

            //
            // Establish principal
            result.Principal = CreatePrincipal(signKey, credential.Scheme);
            result.HasSucceeded = true;

            return result;
        }

        private bool ValidateContext(CredentialValidationResult result)
        {
            if (!_tenant.HmacSha256Enabled)
            {
                result.Error = Errors.SchemeNotAllowed;

                return false;
            }

            return true;
        }

        private bool ValidateSignature(
            HmacToken hmacToken,
            AccessKey key,
            HttpRequest request,
            CredentialValidationResult result)
        {
            if (key == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(hmacToken.Signature))
            {
                result.Error = HmacErrors.SignatureNotFound;

                return false;
            }

            //
            // Token HMAC
            byte[] tokenHmac;

            if (!StringUtils.TryConvertFromBase64String(hmacToken.Signature, out tokenHmac))
            {
                result.Error = HmacErrors.InvalidSignature;

                return false;
            }

            //
            // Expected HMAC
            string stringToSign = CreateStringToSign(hmacToken, request);

            byte[] expectedHmac = Sha256Helper.CalculateHMAC(Encoding.ASCII.GetBytes(stringToSign), key.Secret);

            //
            // Compare
            if (!CryptoUtils.CryptoEquals(expectedHmac, tokenHmac))
            {
                result.Error = HmacErrors.InvalidSignature;

                return false;
            }

            return true;
        }

        private bool ValidateTTL(
            HmacToken token,
            IDictionary<string, StringValues> requestHeaders,
            CredentialValidationResult result)
        {
            //
            // Check MS Date
            if (token.SignedHeaders.Any(h => h.Equals(HeaderNames.MsDate, StringComparison.OrdinalIgnoreCase)))
            {
                return ValidateExpiration(GetMsDate(requestHeaders), result);
            }

            //
            // Check Date
            if (token.SignedHeaders.Any(h => h.Equals(HeaderNames.Date, StringComparison.OrdinalIgnoreCase)))
            {
                return ValidateExpiration(GetDate(requestHeaders), result);
            }

            //
            // Date is required
            result.Error = HmacErrors.InvalidAccessTokenDate;

            return false;
        }

        private bool ValidateExpiration(DateTimeOffset? dt, CredentialValidationResult result)
        {
            if (!dt.HasValue)
            {
                result.Error = HmacErrors.InvalidAccessTokenDate;

                return false;
            }

            //
            // Time offset validity: +/- HmacOptions.AccessTokenExpiration
            if ((DateTimeOffset.UtcNow - dt.Value).Duration() > _options.AccessTokenTTL)
            {
                result.Error = HmacErrors.AccessTokenExpired;

                return false;
            }

            return true;
        }

        private AccessKey ValidateCredential(HmacToken token, CredentialValidationResult result)
        {
            Debug.Assert(token != null);

            if (string.IsNullOrEmpty(token.Credential))
            {
                result.Error = Errors.InvalidCredential;

                return null;
            }

            //
            // Get the sign key
            AccessKey key = _tenant.AccessKeys?.FirstOrDefault(x => x.Id == token.Credential);

            if (key == null)
            {
                result.Error = Errors.InvalidCredential;

                return null;
            }

            return key;
        }

        private bool ValidateSignedHeaders(
            HmacToken token,
            IDictionary<string, StringValues> requestHeaders,
            CredentialValidationResult result)
        {
            if (token.SignedHeaders == null || !token.SignedHeaders.Any())
            {
                result.Error = HmacErrors.SignedHeadersNotFound;
                return false;
            }

            bool hasDate = false;
            bool hasHost = false;
            bool hasContentHash = false;

            foreach (var header in token.SignedHeaders)
            {
                if (string.IsNullOrEmpty(header))
                {
                    result.Error = HmacErrors.InvalidSignedHeaders;
                    return false;
                }

                //
                // Ensure all SignedHeaders are available
                if (!requestHeaders.TryGetValue(header, out StringValues v))
                {
                    result.Error = string.Format(HmacErrors.SignHeaderNotFound, header);
                    return false;
                }

                if (!hasDate)
                {
                    hasDate = header.Equals(HeaderNames.MsDate, StringComparison.OrdinalIgnoreCase) ||
                              header.Equals(HeaderNames.Date, StringComparison.OrdinalIgnoreCase);

                    if (hasDate)
                    {
                        continue;
                    }
                }

                if (!hasHost)
                {
                    hasHost = header.Equals(HeaderNames.Host, StringComparison.OrdinalIgnoreCase);

                    if (hasHost)
                    {
                        continue;
                    }
                }

                if (!hasContentHash)
                {
                    hasContentHash = header.Equals(HeaderNames.MsContentSha256, StringComparison.OrdinalIgnoreCase);
                }
            }

            //
            // Ensure Mandatory SignedHeaders

            //
            // Date or x-ms-date
            if (!hasDate)
            {
                result.Error = string.Format(HmacErrors.SignHeaderNotFound, HeaderNames.Date);
                return false;
            }

            //
            // Host
            if (!hasHost)
            {
                result.Error = string.Format(HmacErrors.SignHeaderNotFound, HeaderNames.Host);
                return false;
            }

            //
            // x-ms-content-sha256
            if (!hasContentHash)
            {
                result.Error = string.Format(HmacErrors.SignHeaderNotFound, HeaderNames.MsContentSha256);
                return false;
            }

            return true;
        }

        private bool ValidateContent(
            string hash,
            IDictionary<string, StringValues> requestHeaders,
            CredentialValidationResult result)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            string hashHeader = GetContentSha256(requestHeaders);

            if (hash != hashHeader)
            {
                result.Error = HmacErrors.InvalidContentHash;
                return false;
            }

            return true;
        }

        private string CreateStringToSign(
            HmacToken token,
            HttpRequest request)
        {
            //
            // Canonical Request:
            //
            // VERB + '\n' +
            // path_and_query_string + '\n' +
            // signed_headers_values

            var sb = new StringBuilder();

            sb.Append(request.Method.ToUpper())
                .Append('\n')
                .Append(request.GetTarget())
                .Append('\n');

            //
            // Append SignedHeaders values
            foreach (var h in token.SignedHeaders)
            {
                if (request.Headers.TryGetValue(h, out StringValues value))
                {
                    sb.Append(value);
                    sb.Append(HmacTokenParser.SignedHeadersDelimiter);
                }
                else
                {
                    //
                    // Fail check
                    // ValidateSignedHeaders should have verified that already
                    throw new Exception($"Request header '{h}' not found. It's required by SignedHeaders.");
                }
            }

            // Remove the last ';'
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        private ClaimsPrincipal CreatePrincipal(AccessKey signKey, string scheme)
        {
            Debug.Assert(signKey != null);
            Debug.Assert(!string.IsNullOrEmpty(scheme));

            //
            // Create claims
            IEnumerable<Claim> claims =
            [
                new Claim(System.Security.Claims.ClaimTypes.Sid, signKey.Id),
                new Claim(System.Security.Claims.ClaimTypes.Role, signKey.ReadOnly ? Roles.Reader : Roles.Owner)
            ];

            //
            // Create principal
            return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
        }

        private static DateTimeOffset? GetDate(IDictionary<string, StringValues> requestHeaders)
        {
            if (requestHeaders.TryGetValue(HeaderNames.Date, out StringValues value))
            {
                if (DateTimeOffset.TryParse(value.FirstOrDefault(), out DateTimeOffset dt))
                {
                    return dt;
                }
            }

            return null;
        }

        private static DateTimeOffset? GetMsDate(IDictionary<string, StringValues> requestHeaders)
        {
            if (requestHeaders.TryGetValue(HeaderNames.MsDate, out StringValues value))
            {
                if (DateTimeOffset.TryParse(value.FirstOrDefault(), out DateTimeOffset dt))
                {
                    return dt;
                }
            }

            return null;
        }

        /// <summary>
        /// Get x-ms-content-sha256 request header value
        /// </summary>
        /// <returns>Base64 encoded hash</returns>
        private static string GetContentSha256(IDictionary<string, StringValues> requestHeaders)
        {
            StringValues value;

            if (!requestHeaders.TryGetValue(HeaderNames.MsContentSha256, out value))
            {
                return null;
            }

            return value.FirstOrDefault();
        }

        /// <summary>
        /// Calculate SHA256 hash of the request body
        /// </summary>
        /// <returns>Base64 encoded hash</returns>
        private static async Task<string> ComputeContentSha256(Stream requestBody, CancellationToken cancellationToken)
        {
            //
            // Calculate hash
            byte[] hash = await Sha256Helper.CalculateHash(requestBody, cancellationToken);

            //
            // Restore Stream position
            requestBody.Position = 0;

            //
            // Base64
            return Convert.ToBase64String(hash);
        }
    }
}
