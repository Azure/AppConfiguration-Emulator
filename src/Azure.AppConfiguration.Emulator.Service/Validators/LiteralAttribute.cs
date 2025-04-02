// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.ComponentModel.DataAnnotations;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    class LiteralAttribute : ValidationAttribute
    {
        public bool NormalizeNull { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var val = value as string;

            if (val == null)
            {
                //
                // Not a string
                return new ValidationResult(validationContext?.MemberName);
            }

            if (NormalizeNull)
            {
                val = SearchQuery.NormalizeNull(val);
            }

            if (val != null)
            {
                int index = FindInvalidCharacter(val);

                if (index >= 0)
                {
                    return new ValidationResult(validationContext?.MemberName);
                }
            }

            return ValidationResult.Success;
        }

        private static int FindInvalidCharacter(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsControl(str[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
