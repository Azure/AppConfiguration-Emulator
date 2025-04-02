using Microsoft.Extensions.Primitives;
using System;
using System.Text;

namespace Azure.AppConfiguration.Emulator.Service.Utils
{
    static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string s1, string s)
        {
            if (ReferenceEquals(s1, s))
            {
                return true;
            }

            if (s1 == null || s == null)
            {
                return false;
            }

            return s1.Equals(s, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this StringSegment s1, string s)
        {
            if (ReferenceEquals(s1.Value, s))
            {
                return true;
            }

            if (s1 == null || s == null)
            {
                return false;
            }

            return s1.Equals(s, StringComparison.OrdinalIgnoreCase);
        }

        public static string Base64Encode(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        public static string Base64Decode(this string base64Value)
        {
            if (string.IsNullOrEmpty(base64Value))
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
        }
    }
}
