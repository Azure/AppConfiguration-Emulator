// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    class LiteralKeyValueAttribute : ValidationAttribute
    {
        private readonly LiteralAttribute _literal = new LiteralAttribute();

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
                if (!(_literal.IsValid(item.Key) && _literal.IsValid(item.Value)))
                {
                    return new ValidationResult($"{validationContext?.MemberName}", new string[] { item.Key.ToString() });
                }
            }

            return ValidationResult.Success;
        }
    }
}
