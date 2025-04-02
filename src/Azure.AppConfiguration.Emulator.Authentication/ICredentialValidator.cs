// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Authentication
{
    public interface ICredentialValidator
    {
        ValueTask<CredentialValidationResult> Validate(Credential token, CancellationToken cancellationToken);

        bool CanValidate(string scheme);

        bool CanChallenge();
    }
}
