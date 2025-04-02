// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service.Http
{
    static class HttpContextExtensions
    {
        private static readonly object KvFieldsKey = new();
        private static readonly object KeyFieldsKey = new();
        private static readonly object LabelFieldsKey = new();
        private static readonly object SnapshotFieldsKey = new();

        public static KeyValueFields GetKeyValueFields(this HttpRequest request)
        {
            if (request.HttpContext.Items.TryGetValue(KvFieldsKey, out object value))
            {
                return (KeyValueFields)value;
            }

            Fields fields = request.GetFields();

            if (!fields.HasFields)
            {
                return KeyValueFields.Default;
            }

            if (fields == Fields.All || fields == Fields.Empty)
            {
                return KeyValueFields.Default;
            }

            KeyValueFields kvf = KeyValueFields.None;

            //
            // etag
            if (fields.Exists("etag"))
            {
                kvf |= KeyValueFields.Etag;
            }

            //
            // key
            if (fields.Exists("key"))
            {
                kvf |= KeyValueFields.Key;
            }

            //
            // label
            if (fields.Exists("label"))
            {
                kvf |= KeyValueFields.Label;
            }

            //
            // content_type
            if (fields.Exists("content_type"))
            {
                kvf |= KeyValueFields.ContentType;
            }

            //
            // value
            if (fields.Exists("value"))
            {
                kvf |= KeyValueFields.Value;
            }

            //
            // last_modified
            if (fields.Exists("last_modified"))
            {
                kvf |= KeyValueFields.LastModified;
            }

            //
            // tags
            if (fields.Exists("tags"))
            {
                kvf |= KeyValueFields.Tags;
            }

            //
            // locked
            if (fields.Exists("locked"))
            {
                kvf |= KeyValueFields.Locked;
            }

            //
            // Cache
            request.HttpContext.Items[KvFieldsKey] = kvf;

            return kvf;
        }

        public static KeyFields GetKeyFields(this HttpRequest request)
        {
            if (request.HttpContext.Items.TryGetValue(KeyFieldsKey, out object value))
            {
                return (KeyFields)value;
            }

            Fields fields = request.GetFields();

            if (!fields.HasFields || fields == Fields.All)
            {
                return KeyFields.All;
            }

            if (fields == Fields.Empty)
            {
                return KeyFields.None;
            }

            KeyFields kf = KeyFields.None;

            //
            // name
            if (fields.Exists("name"))
            {
                kf |= KeyFields.Name;
            }

            //
            // Cache
            request.HttpContext.Items[KeyFieldsKey] = kf;

            return kf;
        }

        public static LabelFields GetLabelFields(this HttpRequest request)
        {
            if (request.HttpContext.Items.TryGetValue(LabelFieldsKey, out object value))
            {
                return (LabelFields)value;
            }

            Fields fields = request.GetFields();

            if (!fields.HasFields || fields == Fields.All)
            {
                return LabelFields.All;
            }

            if (fields == Fields.Empty)
            {
                return LabelFields.None;
            }

            LabelFields lf = LabelFields.None;

            //
            // name
            if (fields.Exists("name"))
            {
                lf |= LabelFields.Name;
            }

            //
            // Cache
            request.HttpContext.Items[LabelFieldsKey] = lf;

            return lf;
        }

        public static SnapshotFields GetSnapshotFields(this HttpRequest request)
        {
            if (request.HttpContext.Items.TryGetValue(SnapshotFieldsKey, out object value))
            {
                return (SnapshotFields)value;
            }

            Fields fields = request.GetFields();

            if (!fields.HasFields)
            {
                return SnapshotFields.All;
            }

            if (fields == Fields.All || fields == Fields.Empty)
            {
                return SnapshotFields.All;
            }

            SnapshotFields snapshotFields = SnapshotFields.None;

            //
            // etag
            if (fields.Exists("etag"))
            {
                snapshotFields |= SnapshotFields.Etag;
            }

            //
            // name
            if (fields.Exists("name"))
            {
                snapshotFields |= SnapshotFields.Name;
            }

            //
            // status
            if (fields.Exists("status"))
            {
                snapshotFields |= SnapshotFields.Status;
            }

            //
            // filters
            if (fields.Exists("filters"))
            {
                snapshotFields |= SnapshotFields.Filters;
            }

            //
            // composition_type
            if (fields.Exists("composition_type"))
            {
                snapshotFields |= SnapshotFields.CompositionType;
            }

            //
            // created
            if (fields.Exists("created"))
            {
                snapshotFields |= SnapshotFields.Created;
            }

            //
            // expires
            if (fields.Exists("expires"))
            {
                snapshotFields |= SnapshotFields.Expires;
            }

            //
            // size
            if (fields.Exists("size"))
            {
                snapshotFields |= SnapshotFields.Size;
            }

            //
            // items_count
            if (fields.Exists("items_count"))
            {
                snapshotFields |= SnapshotFields.ItemsCount;
            }

            //
            // tags
            if (fields.Exists("tags"))
            {
                snapshotFields |= SnapshotFields.Tags;
            }

            //
            // retention_period
            if (fields.Exists("retention_period"))
            {
                snapshotFields |= SnapshotFields.RetentionPeriod;
            }

            //
            // Cache
            request.HttpContext.Items[SnapshotFieldsKey] = snapshotFields;

            return snapshotFields;
        }

        public static Range? GetItemsRange(this HttpRequest request)
        {
            var rangeHeader = request.GetTypedHeaders().Range;

            if (rangeHeader != null &&
                rangeHeader.Unit.EqualsIgnoreCase("items"))
            {
                RangeItemHeaderValue range = rangeHeader.Ranges.FirstOrDefault();

                if (range != null)
                {
                    var from = new Index((int)(range.From ?? 0));
                    var to = new Index((int)(range.To ?? int.MaxValue));

                    return new Range(from, to);
                }
            }

            return null;
        }

        public static void ValidatePrecondition(this HttpRequest request, string resourceEtag)
        {
            if (string.IsNullOrEmpty(resourceEtag))
            {
                throw new ArgumentNullException(nameof(resourceEtag));
            }

            //
            // Validate match condition (if any)
            if (request.TryEvaluatePreconditionStatusCode(resourceEtag, out int _))
            {
                throw new MatchFailedException();
            }
        }

        public static EtagMatch GetEtagMatch(this HttpRequest request)
        {
            if (!string.IsNullOrEmpty(request.GetMatchEtag()))
            {
                return EtagMatch.Match;
            }

            if (!string.IsNullOrEmpty(request.GetNoneMatchEtag()))
            {
                return EtagMatch.NoneMatch;
            }

            return EtagMatch.Ignore;
        }

        public static string GetEtag(this HttpRequest request)
        {
            EtagMatch match = request.GetEtagMatch();

            if (match == EtagMatch.Ignore)
            {
                return null;
            }

            string etag = match == EtagMatch.Match ? request.GetMatchEtag() : request.GetNoneMatchEtag();

            return etag != "*" ? etag : null;
        }

        public static DateTimeOffset? GetTimeGate(this HttpRequest request)
        {
            if (request.Headers.TryGetValue(HeaderNames.AcceptDatetime, out StringValues value))
            {
                if (DateTimeOffset.TryParse(value.FirstOrDefault(), out DateTimeOffset dt))
                {
                    return dt;
                }
            }

            return null;
        }

        public static string GetAfter(this HttpRequest request)
        {
            string next = request.Query["after"];

            if (string.IsNullOrEmpty(next))
            {
                return null;
            }

            try
            {
                return next.Base64Decode();
            }
            catch (FormatException)
            {
                //
                // Ignore the page in case of error
                return null;
            }
        }

        public static string GetTarget(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IHttpRequestFeature requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();

            if (requestFeature == null)
            {
                throw new InvalidOperationException("Missing IHttpRequestFeature instance");
            }

            //
            // RawTarget is null in test server mode
            // fall back to best effort
            return requestFeature.RawTarget ?? new Uri(request.GetEncodedUrl()).PathAndQuery;
        }

        public static string GetForwardedScheme(this HttpRequest request)
        {
            return request.Headers.TryGetValue(HeaderNames.XForwardedProto, out StringValues values) ? values.First() : null;
        }

        public static bool IsRead(this HttpRequest request)
        {
            return HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
        }

        public static void SetRetryAfter(this HttpResponse response, TimeSpan retryAfter)
        {
            string value = Math.Max(1, (long)retryAfter.TotalMilliseconds).ToString();

            response.Headers[HeaderNames.RetryAfterMs] = value;
        }

        public static bool TryEvaluatePreconditionStatusCode(this HttpRequest request, string responseEtag, out int statusCode)
        {
            statusCode = -1;

            EtagMatch requestMatch = request.GetEtagMatch();

            string requestEtag = request.GetEtag();

            //
            // If-Match
            if (requestMatch == EtagMatch.Match)
            {
                if (string.IsNullOrEmpty(requestEtag)) // "*"
                {
                    if (string.IsNullOrEmpty(responseEtag))
                    {
                        statusCode = StatusCodes.Status412PreconditionFailed;
                    }
                }
                else
                {
                    if (requestEtag != responseEtag)
                    {
                        statusCode = StatusCodes.Status412PreconditionFailed;
                    }
                }
            }
            //
            // If-None-Match
            else if (requestMatch == EtagMatch.NoneMatch)
            {
                if (string.IsNullOrEmpty(requestEtag)) // "*"
                {
                    if (!string.IsNullOrEmpty(responseEtag))
                    {
                        statusCode = StatusCodes.Status304NotModified;
                    }
                }
                else
                {
                    if (responseEtag == requestEtag)
                    {
                        statusCode = StatusCodes.Status304NotModified;
                    }
                }
            }

            return statusCode > 0;
        }

        private static Fields GetFields(this HttpRequest request)
        {
            string fieldsQuery = request.Query["$select"];

            //
            // TODO:
            // Backcompat. Remove as appropriate.
            if (fieldsQuery == null)
            {
                fieldsQuery = request.Query["fields"];
            }

            return new Fields(fieldsQuery != null ? fieldsQuery.Split(',') : null);
        }

        private static string GetMatchEtag(this HttpRequest request)
        {
            //
            // If-Match
            var ifMatch = request.GetTypedHeaders().IfMatch;

            if (ifMatch != null && ifMatch.Count > 0)
            {
                return ifMatch[0].Tag.Value.Trim('\"');
            }

            return null;
        }

        private static string GetNoneMatchEtag(this HttpRequest request)
        {
            //
            // If-None-Match
            var ifNoneMatch = request.GetTypedHeaders().IfNoneMatch;

            if (ifNoneMatch != null && ifNoneMatch.Count > 0)
            {
                return ifNoneMatch[0].Tag.Value.Trim('\"');
            }

            return null;
        }
    }
}
