// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Authentication
{
    public class Credential
    {
        public string Scheme { get; set; }

        public string Value { get; set; }

        public string Host { get; set; }
    }
}
