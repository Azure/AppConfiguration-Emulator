// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppConfig.Service.Authorization
{
    public class AuthorizationDecision
    {
        public string AccessDecision { get; set; }

        public string ActionId { get; set; }

        public TimeSpan TTL { get; set; }
    }
}
