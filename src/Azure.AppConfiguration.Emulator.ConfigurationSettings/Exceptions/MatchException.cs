// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class MatchFailedException : Exception
    {
        public MatchFailedException(Exception inner = null) : base(string.Empty, inner)
        {
        }
    }
}
