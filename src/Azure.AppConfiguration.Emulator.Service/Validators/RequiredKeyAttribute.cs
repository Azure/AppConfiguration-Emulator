// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    class RequiredKeyAttribute : ValidationAttribute
    {
        private readonly RequiredAttribute _required = new RequiredAttribute();

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var dict = value as IDictionary;

            if (dict == null)
            {
                return new ValidationResult(validationContext?.MemberName);
            }

            foreach (DictionaryEntry item in dict)
            {
                if (!_required.IsValid(item.Key))
                {
                    return new ValidationResult($"{validationContext?.MemberName}.{item.Key}");
                }
            }

            return ValidationResult.Success;
        }
    }
}
