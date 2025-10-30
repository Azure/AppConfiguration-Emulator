// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IKeyValueStorage
    {
        Task AppendKeyValue(KeyValue keyValue, CancellationToken cancellationToken);

        IAsyncEnumerable<KeyValue> QueryKeyValues(CancellationToken cancellationToken);

        Task Save(IEnumerable<KeyValue> keyValues, CancellationToken cancellationToken);
    }
}
