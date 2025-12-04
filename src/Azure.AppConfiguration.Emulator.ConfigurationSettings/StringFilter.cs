using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public struct StringFilter
    {
        private static readonly StringFilter _nullString = new StringFilter { IsNull = true };

        public bool IsNull { get; set; }

        public string EqualsTo { get; set; }

        public string Prefix { get; set; }

        public IEnumerable<string> AnyOf { get; set; }

        public bool Match(string value)
        {
            if (IsNull)
            {
                return value == null;
            }

            if (EqualsTo != null &&
                (value == null || !EqualsTo.Equals(value)))
            {
                return false;
            }

            if (Prefix != null &&
                (value == null || !value.StartsWith(Prefix)))
            {
                return false;
            }

            if (AnyOf != null && !AnyOf.Contains(value))
            {
                return false;
            }

            return true;
        }

        public bool IsEmpty => !IsNull && Match(null);

        public static StringFilter NullString => _nullString;
    }
}
