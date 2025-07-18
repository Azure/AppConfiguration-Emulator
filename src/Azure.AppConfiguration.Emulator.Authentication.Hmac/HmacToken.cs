// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AppConfig.Service.Security.Authentication.Hmac
{
    internal class HmacToken
    {
        public string Credential { get; set; }

        //
        // Base64 encoded
        public string Signature { get; set; }

        public IEnumerable<string> SignedHeaders { get; set; }
    }
}
