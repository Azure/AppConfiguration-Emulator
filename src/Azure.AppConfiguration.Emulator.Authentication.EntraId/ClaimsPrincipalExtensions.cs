// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppConfig.Service.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;

using ClaimTypes = Microsoft.AppConfig.Service.Authorization.ClaimTypes;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public static class ClaimsPrincipalExtensions
    {
        public static void AddAuthorizationIdentity(this ClaimsPrincipal principal, IEnumerable<AuthorizationDecision> rbacDecisions)
        {
            const string ActionAllowed = "Allowed";

            var identity = new ClaimsIdentity();

            //
            // Action claims
            if (rbacDecisions != null)
            {
                bool isScoped = principal.HasClaim(c => c.Type == ClaimTypes.ActionScope);

                foreach (AuthorizationDecision ad in rbacDecisions)
                {
                    if (ad.AccessDecision.Equals(ActionAllowed, StringComparison.OrdinalIgnoreCase) &&
                        (!isScoped || principal.HasClaim(c => c.Type == ClaimTypes.ActionScope && c.Value == ad.ActionId)))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.AllowAction, ad.ActionId));
                    }
                }
            }

            //
            // Sid claim
            string oid = principal.Claims.GetObjectId();

            if (!string.IsNullOrEmpty(oid))
            {
                identity.AddClaim(new Claim(System.Security.Claims.ClaimTypes.Sid, oid));
            }

            principal.AddIdentity(identity);
        }
    }
}
