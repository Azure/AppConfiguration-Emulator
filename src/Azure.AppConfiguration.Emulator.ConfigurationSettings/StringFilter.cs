using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public struct StringFilter
    {
        public bool IsNull { get; set; }

        public string EqualsTo { get; set; }

        public string HasPrefix { get; set; }

        public IEnumerable<string> AnyOf { get; set; }

        public bool Match(string value)
        {
            if (IsNull)
            {
                return value == null;
            }

            if (EqualsTo != null && !EqualsTo.Equals(value))
            {
                return false;
            }

            if (HasPrefix != null && !value.StartsWith(HasPrefix))
            {
                return false;
            }

            if (AnyOf != null && !AnyOf.Contains(value))
            {
                return false;
            }

            return true;
        }
    }
}
