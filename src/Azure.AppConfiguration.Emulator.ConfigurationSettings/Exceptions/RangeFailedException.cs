// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class RangeFailedException : Exception
    {
        public RangeFailedException() : base()
        {
        }

        public RangeFailedException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
