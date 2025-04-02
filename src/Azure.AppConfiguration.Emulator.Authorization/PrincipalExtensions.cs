// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AppConfig.Service.Authorization
{
    static class PrincipalExtensions
    {
        public static bool AllowAction(this ClaimsPrincipal principal, string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            return principal.HasClaim(c => c.Type == ClaimTypes.AllowAction && c.Value == action);
        }
    }
}
