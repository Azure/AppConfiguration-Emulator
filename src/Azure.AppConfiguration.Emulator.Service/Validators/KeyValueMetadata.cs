// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppConfig.Service;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    class KeyValueMetadata
    {
        [Literal]
        public string ContentType { get; set; }

        [RequiredKey]
        [MaxLengthKeyValue(DataModelConstraints.MaxTagNameLength, DataModelConstraints.MaxTagValueLength)]
        [LiteralKeyValue]
        public IDictionary<string, string> Tags { get; set; }
    }
}
