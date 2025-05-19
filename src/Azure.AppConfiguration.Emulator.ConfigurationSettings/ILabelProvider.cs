// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface ILabelProvider
    {
        ValueTask<Page<Label>> QueryLabels(
            LabelSearchOptions options,
            CancellationToken cancellationToken);
    }
}
