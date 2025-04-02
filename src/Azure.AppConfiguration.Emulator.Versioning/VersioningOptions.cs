// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Versioning
{
    public class VersioningOptions
    {
        /// <summary>
        /// Default API version (semver major.minor)
        /// Used if the client didn't specify api-version
        /// 
        /// IMPORTANT: 
        /// Modifing this in prod may affect clients that don't explicitly specify api version
        /// Removing the default version will result in HTTP 400 for requests that don't specify api version
        /// </summary>
        public string DefaultApiVersion { get; set; } = ApiVersions.V1;

        /// <summary>
        /// Collection of API versions that are supported.
        /// If an API version specified by a client is NOT in this collection, the request is rejected 
        /// with "Unsupported API Version" by the ConditionalVersioningFilter.
        /// </summary>
        public IEnumerable<ApiVersionRule> AllowedApiVersions { get; set; }
    }
}
