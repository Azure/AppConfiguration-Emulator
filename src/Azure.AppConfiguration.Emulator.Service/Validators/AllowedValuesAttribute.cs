using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class AllowedValuesAttribute : ValidationAttribute
    {
        public object[] Values { get; }

        public StringComparison ComparisonStrategy { get; set; }

        public AllowedValuesAttribute(params object[] values)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string val)
            {
                if (!Values.Any(v => v is string strValue && string.Equals(val, strValue, ComparisonStrategy)))
                {
                    return new ValidationResult(validationContext?.MemberName);
                }
            }
            else
            {
                if (value != null && !Values.Any(v => Equals(v, value)))
                {
                    return new ValidationResult(validationContext?.MemberName);
                }
            }

            return ValidationResult.Success;
        }
    }
}
