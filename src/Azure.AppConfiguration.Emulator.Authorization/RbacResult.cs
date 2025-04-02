// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AppConfig.Service.Authorization
{
    public struct RbacResult
    {
        public AuthorizationError Error { get; set; }

        public IEnumerable<AuthorizationDecision> Decisions { get; set; }
    }
}
