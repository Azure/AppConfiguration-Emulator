// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    class IgnoreBindingAttribute : Attribute, IBindingSourceMetadata
    {
        public IgnoreBindingAttribute(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }

        public BindingSource BindingSource { get; } = BindingSource.Custom;
    }
}
