// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AppConfig.Service.Security.Authentication.Hmac
{
    internal static class HmacTokenParser
    {
        public const char SignedHeadersDelimiter = ';';
        private const char NameValueDelimiter = '=';

        private const string Credential = "Credential";
        private const string Signature = "Signature";
        private const string SignedHeaders = "SignedHeaders";

        /// <summary>
        /// Parses Authorization HTTP Header Credential for HMAC authentication 
        /// (see https://github.com/Azure/AppConfiguration/blob/master/docs/REST/authentication/hmac.md)
        /// </summary>
        /// <param name="token">
        /// Expected format:
        /// Credential={credential}&SignedHeaders={header1};{header2};..;{headerN}&Signature={signature}
        /// </param>
        /// <returns></returns>
        public static HmacToken Parse(string token)
        {
            var hmacToken = new HmacToken();

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            int i = 0;

            while (i < token.Length)
            {
                char c = token[i];

                if (char.IsWhiteSpace(c) || IsTokenDelimiter(c))
                {
                    ++i;
                    continue;
                }

                ParseToken(token.AsSpan(i), out ReadOnlySpan<char> name, out ReadOnlySpan<char> value);

                i += name.Length + 1;

                if (value.Length > 0)
                {
                    i += value.Length + 1;
                }

                //
                // Credential
                if (hmacToken.Credential == null && Equals(name, Credential))
                {
                    hmacToken.Credential = value.ToString();
                    continue;
                }

                //
                // SignedHeaders
                if (hmacToken.SignedHeaders == null && Equals(name, SignedHeaders))
                {
                    hmacToken.SignedHeaders = Split(value, SignedHeadersDelimiter);
                    continue;
                }

                //
                // Signature
                if (hmacToken.Signature == null && Equals(name, Signature))
                {
                    hmacToken.Signature = value.ToString();
                    continue;
                }
            }

            return hmacToken;
        }

        private static void ParseToken(ReadOnlySpan<char> span, out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            name = span;
            value = ReadOnlySpan<char>.Empty;

            for (int i = 0; i < span.Length; ++i)
            {
                char c = span[i];

                //
                // Name
                if (name == span)
                {
                    if (c == NameValueDelimiter)
                    {
                        name = span.Slice(0, i);
                    }
                    else if (IsTokenDelimiter(c))
                    {
                        name = span.Slice(0, i);
                        break;
                    }
                }
                //
                // Value
                else
                {
                    if (IsTokenDelimiter(c))
                    {
                        value = span.Slice(name.Length + 1, i - name.Length - 1);
                        break;
                    }
                    else if (i == span.Length - 1)
                    {
                        value = span.Slice(name.Length + 1);
                    }
                }
            }
        }

        private static IEnumerable<string> Split(ReadOnlySpan<char> span, char separator)
        {
            var values = new List<string>();

            while (span.Length > 0)
            {
                int pos = span.IndexOf(separator);

                if (pos < 0)
                {
                    pos = span.Length;
                }

                values.Add(span.Slice(0, pos).Trim().ToString());

                ++pos;

                span = (pos < span.Length) ? span.Slice(pos) : ReadOnlySpan<char>.Empty;
            }

            return values;
        }

        private static bool IsTokenDelimiter(char c)
        {
            //
            // Using comma in HTTP header value is against RFC. 
            // see https://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.2
            // The support here is only for backcompat purpose.

            return c == '&' || c == ',';
        }

        private static bool Equals(ReadOnlySpan<char> left, string right)
        {
            return left.Equals(right.AsSpan(), StringComparison.Ordinal);
        }
    }
}
