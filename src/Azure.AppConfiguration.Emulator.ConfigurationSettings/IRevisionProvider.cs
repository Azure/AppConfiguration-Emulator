// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IRevisionProvider
    {
        Task<IEnumerable<KeyValue>> Get(KeyValueSearchOptions options, CancellationToken cancellationToken);
    }
}
