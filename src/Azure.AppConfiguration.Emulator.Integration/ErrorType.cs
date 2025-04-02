// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Integration
{
    public static class ErrorType
    {
        private const string Namespace = "https://azconfig.io/errors";

        public static readonly string KeyLocked = $"{Namespace}/key-locked";
        public static readonly string InvalidArgument = $"{Namespace}/invalid-argument";
        public static readonly string AlreadyExists = $"{Namespace}/already-exists";
        public static readonly string InvalidState = $"{Namespace}/invalid-state";
        public static readonly string ServerError = $"{Namespace}/server-error";
    }
}
