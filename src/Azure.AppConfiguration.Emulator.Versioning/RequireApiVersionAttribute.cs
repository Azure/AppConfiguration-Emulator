// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Azure.AppConfiguration.Emulator.Versioning
{
    [AttributeUsage(AttributeTargets.All)]
    public class RequireApiVersionAttribute : ValidationAttribute
    {
        public RequireApiVersionAttribute(string minApiVersion, bool required = false)
        {
            MinApiVersion = ApiVersion.Parse(minApiVersion ?? throw new ArgumentNullException(nameof(minApiVersion)));
            Required = required;
        }

        public ApiVersion MinApiVersion { get; private set; }

        public bool Required { get; private set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                // 
                // Nothing to validate
                return ValidationResult.Success;
            }

            if (Required && IsNullOrEmptyObject(value))
            {
                IHttpContextAccessor httpAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));

                ApiVersion apiVersion = httpAccessor?.HttpContext?.GetRequestedApiVersion();

                if (apiVersion == null)
                {
                    return new ValidationResult(ErrorCodes.ApiVersionUnspecified);
                }

                if (apiVersion >= MinApiVersion)
                {
                    return new ValidationResult(ErrorCodes.ErrorMissingRequiredProperty);
                }
            }

            return ValidationResult.Success;
        }

        private static bool IsNullOrEmptyObject(object value)
        {
            switch (value)
            {
                case null:
                    return true;

                case string str:
                    return string.IsNullOrEmpty(str);

                case IEnumerable enumerable:
                    return !enumerable.GetEnumerator().MoveNext();

                default:
                    return false;
            }
        }
    }
}
