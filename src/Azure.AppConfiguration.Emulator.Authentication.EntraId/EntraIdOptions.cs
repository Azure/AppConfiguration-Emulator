// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public class EntraIdOptions
    {
        /// <summary>
        /// Active Directory endpoint (cloud specific)
        /// ex. https://login.microsoftonline.com/
        /// </summary>
        public string ActiveDirectoryEndpoint { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>
        /// Enables the use of regional AAD endpoints
        /// </summary>
        public bool EnableRegionalAadEndpoints { get; set; } = true;

        /// <summary>
        /// Aad audience (cloud specific)
        /// ex. https://azconfig.io
        /// </summary>
        public IEnumerable<string> GlobalAudiences { get; set; } = [
            "https://azconfig.io" // Public cloud
        ];

        /// <summary>
        /// The maximum time period before refresh
        /// </summary>
        public TimeSpan OpenIdMaxStaleInterval = TimeSpan.FromDays(1);

        /// <summary>
        /// The minimum time period before refresh
        /// </summary>
        public TimeSpan OpenIdMinRefreshInterval = TimeSpan.FromMinutes(5); // The minimum time period before refresh
    }
}
