// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IKeyValueProvider
    {
        ValueTask<Page<KeyValue>> QueryKeyValues(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken);

        ValueTask<KeyValue> GetKeyValue(
            string key,
            string label,
            CancellationToken cancellationToken);

        ValueTask<KeyValue> Set(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask Remove(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask<KeyValue> Lock(
            KeyValue kv,
            CancellationToken cancellationToken);

        ValueTask<KeyValue> Unlock(
            KeyValue kv,
            CancellationToken cancellationToken);
    }
}
