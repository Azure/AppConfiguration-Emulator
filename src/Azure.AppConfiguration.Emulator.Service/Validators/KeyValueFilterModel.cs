// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Versioning;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public class KeyValueFilterModel
    {
        public string Key { get; set; }

        public string Label { get; set; }

        [RequireApiVersion(minApiVersion: ApiVersions.V23_11)]
        public IEnumerable<string> Tags { get; set; }
    }
}
