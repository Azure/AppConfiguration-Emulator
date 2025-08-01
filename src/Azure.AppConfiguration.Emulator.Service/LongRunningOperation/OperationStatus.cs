// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Service.LongRunningOperation
{
    class OperationStatus
    {
        public string Id { get; set; }

        public Status Status { get; set; }

        public ErrorDetail Error { get; set; }
    }
}
