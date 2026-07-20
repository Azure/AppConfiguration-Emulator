// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Azure.AppConfiguration.Emulator.Versioning;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [ModelMetadataType(typeof(KeyValueMetadata))]
    public class KeyValueModel
    {
        /// <summary>
        /// Content type of key value.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Value of key value. 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Description of key value.
        /// </summary>
        [RequireApiVersion(minApiVersion: ApiVersions.V26_04)]
        public string Description { get; set; }

        /// <summary>
        /// Tags associated with key value.
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
    }
}
