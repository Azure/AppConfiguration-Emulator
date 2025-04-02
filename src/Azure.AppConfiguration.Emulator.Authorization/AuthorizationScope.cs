// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppConfig.Service.Authorization
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizationScope : Attribute
    {
        public ResourceType ResourceType { get; set; }
    }
}
