// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    class MaxLengthKeyValueAttribute : ValidationAttribute
    {
        private readonly MaxLengthAttribute _keyMaxLength;
        private readonly MaxLengthAttribute _valueMaxLength;

        public MaxLengthKeyValueAttribute(int keyLength, int valueLength)
        {
            if (keyLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keyLength));
            }

            if (valueLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valueLength));
            }

            _keyMaxLength = new MaxLengthAttribute(keyLength);
            _valueMaxLength = new MaxLengthAttribute(valueLength);
        }

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
                if (!(_keyMaxLength.IsValid(item.Key) &&
                      _valueMaxLength.IsValid(item.Value)))
                {
                    return new ValidationResult($"{validationContext?.MemberName}.{item.Key}");
                }
            }

            return ValidationResult.Success;
        }
    }
}
