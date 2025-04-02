// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppConfig.Service.Authentication
{
    public class HmacOptions
    {
        /// <summary>
        /// Defines AccessToken acceptable time interval
        /// It's necessary to account for the clock skew between the issuer and the validator.
        /// The valid interval is: Now +/- AccessTokenTTL
        /// </summary>
        public TimeSpan AccessTokenTTL { get; init; } = TimeSpan.FromMinutes(15);
    }
}
