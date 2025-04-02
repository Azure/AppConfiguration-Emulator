using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Service.Utils
{
    static class SearchQueryHelper
    {
        private const string TagsParameterName = "tags";
        private const char TagFilterSeparator = '=';

        public static KeyValuePair<string, string> ParseTagFilter(ReadOnlySpan<char> query)
        {
            if (query.IsWhiteSpace())
            {
                throw new ArgumentNullException(nameof(query));
            }

            string tagName = null;
            string tagValue = null;
            bool tagNameExtracted = false;
            bool tagValueExtracted = false;
            int offset = 0;

            //
            // Split on '='
            SearchQuery.SplitEscaped(query, TagFilterSeparator, (span) =>
            {
                if (tagNameExtracted && tagValueExtracted)
                {
                    throw new SearchQueryException(TagsParameterName, $"Invalid tag filter with multiple '{TagFilterSeparator}' separators.");
                }

                if (!tagNameExtracted)
                {
                    //
                    // TagName cannot be null, empty or '\0'
                    if (span.Equals(SearchQuery.NullString.AsSpan(), StringComparison.Ordinal) || span.IsWhiteSpace())
                    {
                        throw new SearchQueryException(TagsParameterName, $"Invalid tag filter with null or empty tag name.");
                    }

                    tagName = SearchQuery.Unescape(span, offset).ToString();
                    tagNameExtracted = true;
                }
                else if (!tagValueExtracted)
                {
                    //
                    // To search for tags with null values,
                    // request must be in the format: tagName=\0
                    tagValue = span.Equals(SearchQuery.NullString.AsSpan(), StringComparison.Ordinal)
                        ? null
                        : SearchQuery.Unescape(span, offset).ToString();

                    tagValueExtracted = true;
                }

                offset += span.Length + 1;

                return true;
            });

            if (!tagNameExtracted || !tagValueExtracted)
            {
                throw new SearchQueryException(TagsParameterName, "Invalid tag filter");
            }

            return new KeyValuePair<string, string>(tagName, tagValue);
        }
    }
}
