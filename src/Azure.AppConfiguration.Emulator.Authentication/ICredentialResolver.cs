// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Authentication
{
    public interface ICredentialResolver
    {
        Credential GetCredential();
    }
}
