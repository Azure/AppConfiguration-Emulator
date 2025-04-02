// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RouteQueryAttribute : Attribute, IActionConstraint
    {
        private readonly string _parameterName;

        public RouteQueryAttribute(string parameterName)
        {
            _parameterName = parameterName ?? throw new ArgumentNullException();
        }

        public bool Accept(ActionConstraintContext context)
        {
            return context.RouteContext.HttpContext.Request.Query.Keys.Contains(_parameterName);
        }

        public int Order { get; }
    }
}
