// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IKeyValueProvider
    {
        ValueTask<Page<KeyValue>> Get(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken);

        ValueTask<KeyValue> Get(
            string key,
            string label,
            CancellationToken cancellationToken);

        ValueTask Set(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask Remove(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask Lock(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask Unlock(
            KeyValue kv,
            CancellationToken cancellationToken);
    }
}
