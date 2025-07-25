// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    class RequestArgumentException : ArgumentException
    {
        public RequestArgumentException(string paramName, Exception inner = null) : base(string.Empty, paramName, inner)
        {
        }
    }
}
