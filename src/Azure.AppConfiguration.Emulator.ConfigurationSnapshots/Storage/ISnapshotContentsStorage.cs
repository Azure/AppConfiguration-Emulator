// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public interface ISnapshotContentsStorage
    {
        Task<MediaInfo> CreateContent(string fileName, IEnumerable<KeyValue> items, CancellationToken cancellationToken);

        IAsyncEnumerable<KeyValue> GetContent(MediaInfo media, long offset, CancellationToken cancellationToken);
    }
}
