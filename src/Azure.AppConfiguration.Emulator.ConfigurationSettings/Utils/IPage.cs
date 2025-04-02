// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IPage
    {
        long TotalItemsCount { get; }

        long Offset { get; }

        int Count { get; }

        string ContinuationToken { get; }

        string NextLink { get; set; }

        string Etag { get; }
    }
}
