// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    [Flags]
    public enum LabelFields
    {
        None = 0,

        Name = 0x001,

        All = Name
    }
}
