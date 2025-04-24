// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public static class SearchQuery
    {
        public const string NullString = "\0";
        public const char EscapeChar = '\\';

        private const int MaxListSearchItems = 5;
        private const char SearchListSeparator = ',';
        private const char Wildcard = '*';

        private const string ReservedChars = "*,\\"; // Only ASCII characters (0-127) can be reserved!

        private static readonly byte[] _reservedCharLookup = CreateReservedCharLookup();

        public static readonly Func<char, bool, bool> IsInvalidCharacter = (c, escaped) => !escaped && IsReservedChar(c);

        public static StringFilter CreateStringFilter(string value)
        {
            StringFilter filter = new();

            //
            // No filter
            if (string.IsNullOrEmpty(value) || value[0] == Wildcard)
            {
                return filter;
            }

            //
            // Null
            if (value == NullString)
            {
                filter.IsNull = true;

                return filter;
            }

            //
            // List of values
            filter.AnyOf = ParseList(value);

            if (filter.AnyOf != null)
            {
                return filter;
            }

            //
            // Starts With
            filter.Prefix = StartsWithSearch(value);

            if (!string.IsNullOrEmpty(filter.Prefix))
            {
                return filter;
            }

            //
            // Specific value
            filter.EqualsTo = Unescape(value).ToString();

            return filter;
        }

        /// <summary>
        /// Escape reserved characters defined in ReservedChars
        /// </summary>
        /// <param name="value">Unescaped value</param>
        /// <param name="additionalReservedChar">Character to be escaped, in addition to ReservedChars.</param>
        /// <returns>Escaped value</returns>
        public static string Escape(string value, char? additionalReservedChar = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder sb = null;
            int pos = 0;

            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (IsReservedChar(c) || c == additionalReservedChar)
                {
                    //
                    // Optimisticly provide 10% buffer overhead due to special chars
                    sb = sb ?? new StringBuilder((int)Math.Ceiling(value.Length * 1.1));

                    sb.Append(value.Substring(pos, i - pos))
                      .Append(EscapeChar)
                      .Append(c);

                    pos = i + 1;
                }
            }

            if (sb != null)
            {
                //
                // Append the remaining
                if (pos < value.Length)
                {
                    sb.Append(value.Substring(pos));
                }

                return sb.ToString();
            }

            return value;
        }

        /// <summary>
        /// Checks if the {index} belongs to escape sequence
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsCharEscaped(ReadOnlySpan<char> value, int index)
        {
            if (index < 0 || index >= value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (value[index] == EscapeChar)
            {
                return true;
            }

            int count = 0;

            while (--index >= 0 && value[index] == EscapeChar)
            {
                ++count;
            }

            //
            // An odd number of escape chars tells that the current index is part of escape sequence
            return count % 2 != 0;
        }

        /// <summary>
        /// Find the index of the first character mathing the predicate
        /// </summary>
        /// <param name="value">String to search</param>
        /// <param name="predicate">(char c, bool escaped)</param>
        /// <returns></returns>
        public static int IndexOf(ReadOnlySpan<char> value, Func<char, bool, bool> predicate)
        {
            int leadingEscapeCharsCount = 0;

            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (c != EscapeChar)
                {
                    //
                    // An odd number of leading escape chars tells that the current char is escaped
                    if (predicate(c, leadingEscapeCharsCount % 2 != 0))
                    {
                        return i;
                    }

                    leadingEscapeCharsCount = 0;
                }
                else
                {
                    ++leadingEscapeCharsCount;
                }
            }

            return -1;
        }

        public delegate bool SplitCallback(ReadOnlySpan<char> span);

        /// <summary>
        /// Splits an escaped string
        /// Escaped separator occurrences are ignorred.
        /// </summary>
        /// <param name="value">escaped string</param>
        /// <param name="separator">Character to split on</param>
        /// <param name="callback">Callback to receive a segment</param>
        public static void SplitEscaped(ReadOnlySpan<char> value, char separator, SplitCallback callback)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (separator == EscapeChar)
            {
                throw new ArgumentException(nameof(separator));
            }

            if (callback == null)
            {
                throw new ArgumentException(nameof(callback));
            }

            int pos = 0;

            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (c == EscapeChar)
                {
                    ++i;
                    continue;
                }

                if (c == separator)
                {
                    if (!callback(value.Slice(pos, i - pos)))
                    {
                        return;
                    }

                    pos = i + 1;
                }
            }

            //
            // Append the remaining
            if (pos > 0 && pos <= value.Length)
            {
                callback(value.Slice(pos, value.Length - pos));
            }
        }

        public static bool IsNullOrZero(string value)
        {
            return value == null || value == NullString;
        }

        public static string NormalizeNull(string value)
        {
            return IsNullOrZero(value) ? null : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReservedChar(char c)
        {
            return c >= 0 &&
                    c < _reservedCharLookup.Length &&
                    _reservedCharLookup[c] == 1;
        }

        public static bool ContainsWildcard(ReadOnlySpan<char> search)
        {
            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            for (int i = 0; i < search.Length; i++)
            {
                if (search[i] == Wildcard && !IsCharEscaped(search, i))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsListSearch(ReadOnlySpan<char> search)
        {
            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            for (int i = 0; i < search.Length; i++)
            {
                if (search[i] == SearchListSeparator && !IsCharEscaped(search, i))
                {
                    return true;
                }
            }

            return false;
        }

        public static ReadOnlySpan<char> Unescape(ReadOnlySpan<char> value, int offset = 0)
        {
            //
            // Validate
            int i = IndexOf(value, IsInvalidCharacter);

            if (i >= 0)
            {
                throw new SearchQueryException(nameof(value), "Invalid character")
                {
                    Position = offset + i
                };
            }

            return UnescapeInternal(value);
        }

        /// <summary>
        /// Unescape
        /// </summary>
        /// <param name="value">Escaped value</param>
        /// <returns>Unescaped value</returns>
        private static ReadOnlySpan<char> UnescapeInternal(ReadOnlySpan<char> value)
        {
            var buffer = new Span<char>();
            int bufferIndex = 0;
            int pos = 0;

            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] == EscapeChar)
                {
                    if (buffer.Length == 0)
                    {
                        // Unescaped sequence is always <= escaped
                        buffer = new Span<char>(new char[value.Length]);
                    }

                    // Append a segment
                    ReadOnlySpan<char> slice = value.Slice(pos, i - pos);
                    slice.CopyTo(buffer.Slice(bufferIndex, slice.Length));
                    bufferIndex += slice.Length;

                    ++i;
                    pos = i + 1;

                    // Append next char
                    if (i < value.Length)
                    {
                        buffer[bufferIndex] = value[i];
                        ++bufferIndex;
                    }
                }
            }

            if (buffer.Length > 0)
            {
                //
                // Append the remaining
                if (pos < value.Length)
                {
                    ReadOnlySpan<char> slice = value.Slice(pos);
                    slice.CopyTo(buffer.Slice(bufferIndex));
                    bufferIndex += slice.Length;
                }

                value = buffer.Slice(0, bufferIndex);
            }

            return value;
        }

        /// <summary>
        /// Create lookup table to perform quick search for reserved characters
        /// </summary>
        /// <returns></returns>
        private static byte[] CreateReservedCharLookup()
        {
            byte[] lookup = new byte[128];

            for (int i = 0; i < ReservedChars.Length; ++i)
            {
                Debug.Assert(ReservedChars[i] < lookup.Length);

                lookup[ReservedChars[i]] = 1;
            }

            return lookup;
        }

        private static IEnumerable<string> ParseList(ReadOnlySpan<char> query)
        {
            Debug.Assert(!query.IsEmpty);

            //
            // Split on ','
            List<string> values = null;
            int offset = 0;

            SearchQuery.SplitEscaped(
                query,
                SearchListSeparator,
                (span) =>
                {
                    values ??= new List<string>();

                    if (values.Count >= MaxListSearchItems)
                    {
                        throw new SearchQueryException(nameof(query), $"Too many values. Maximum supported is {MaxListSearchItems}");
                    }

                    if (span.SequenceEqual(NullString))
                    {
                        values.Add(null);
                    }
                    else
                    {
                        values.Add(Unescape(span, offset).ToString());
                    }

                    offset += span.Length + 1;

                    return true;
                });

            return values;
        }

        private static string StartsWithSearch(ReadOnlySpan<char> query)
        {
            Debug.Assert(!query.IsEmpty);

            bool suffixWildcard = query[query.Length - 1] == Wildcard && !IsCharEscaped(query, query.Length - 1);

            if (suffixWildcard)
            {
                query = query.Slice(0, query.Length - 1);

                return Unescape(query, 0).ToString();
            }

            return null;
        }
    }
}
