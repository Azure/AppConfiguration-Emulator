using System;

namespace Microsoft.AppConfig.Service.Authorization
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class NameAttribute : Attribute
    {
        public NameAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = value;
        }

        public string Name { get; private set; }
    }
}
