// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    class CheckAccessArgs
    {
        public string ObjectId { get; set; }

        public IEnumerable<string> Groups { get; set; }

        public IEnumerable<string> Actions { get; set; }

        public string ResourceId { get; set; }

        /// <summary>
        /// Flattened value from JWT
        /// </summary>
        public string ClaimNames { get; set; }

        /// <summary>
        /// Flattened value from JWT
        /// </summary>
        public string ClaimSources { get; set; }
    }
}
