// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public class AuthorizationProvider : IAuthorizationProvider
    {
        private readonly TenantOptions _tenant;

        public AuthorizationProvider(
            IOptions<TenantOptions> tenant,
            ILoggerFactory logFactory)
        {
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Access Control (RBAC) for the provided principal and actions.
        /// </summary>
        /// <param name="principal">Principal to check</param>
        /// <param name="actions">The principal is checked against the provided list of actions</param>
        /// <param name="cancellationToken"></param>
        /// <returns>RBAC result</returns>
        public ValueTask<RbacResult> CheckAccess(
            ClaimsPrincipal principal,
            IEnumerable<string> actions,
            CancellationToken cancellationToken)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            throw new NotImplementedException();
        }
    }
}
