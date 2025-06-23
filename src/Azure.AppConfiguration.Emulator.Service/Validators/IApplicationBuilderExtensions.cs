// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Builder;
using System;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public static partial class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePathValidation(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<PathValidatorMiddleware>();
        }
    }
}
