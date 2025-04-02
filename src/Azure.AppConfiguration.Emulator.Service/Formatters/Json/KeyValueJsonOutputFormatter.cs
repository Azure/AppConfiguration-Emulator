// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Formatters.Serializer;
using Azure.AppConfiguration.Emulator.Service.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    sealed class KeyValueJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        private readonly IReadOnlyDictionary<ApiVersion, IOuputSerializer<KeyValue>> _serializers;

        public KeyValueJsonOutputFormatter(
            JsonSerializerSettings settings,
            ArrayPool<char> charPool,
            MvcOptions mvcOptions,
            params IOuputSerializer<KeyValue>[] serializers)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
            _serializers = serializers.MapApiVersionAttribute();

            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.KeyValueApplication);
        }

        protected override bool CanWriteType(Type type)
        {
            //
            // Supported types 
            if (typeof(KeyValue).IsAssignableFrom(type))
            {
                return base.CanWriteType(type);
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext ctx, Encoding selectedEncoding)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            HttpRequest request = ctx.HttpContext.Request;
            HttpResponse response = ctx.HttpContext.Response;

            if (ctx.Object == null ||
                response.StatusCode == StatusCodes.Status304NotModified ||
                response.StatusCode == StatusCodes.Status412PreconditionFailed ||
                HttpMethods.IsHead(request.Method))
            {
                return;
            }

            //
            // Get serializer
            IOuputSerializer<KeyValue> serializer = GetSerializer(ctx.HttpContext);

            KeyValueFields fields = request.GetKeyValueFields();

            using (var writer = ctx.WriterFactory(response.Body, selectedEncoding))
            using (JsonWriter jw = CreateJsonWriter(writer))
            {
                await serializer.WriteContent(jw, (KeyValue)ctx.Object, (long)fields);

                // Explicitly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            KeyValue kv = (KeyValue)context.Object;

            //
            // Handle conditional request
            if (context.HttpContext.Request.IsRead() &&
                context.HttpContext.Request.TryEvaluatePreconditionStatusCode(kv?.Etag, out int statusCode))
            {
                context.HttpContext.Response.StatusCode = statusCode;
                return;
            }

            base.WriteResponseHeaders(context);

            if (kv == null)
            {
                return;
            }

            //
            // Get serializer
            IOuputSerializer<KeyValue> serializer = GetSerializer(context.HttpContext);

            //
            // Write headers
            serializer.WriteResponseHeaders(context.HttpContext.Response, kv);
        }

        private IOuputSerializer<KeyValue> GetSerializer(HttpContext context)
        {
            Debug.Assert(context != null);

            //
            // Get ApiVersion
            ApiVersion apiVersion = context.GetRequestedApiVersion();

            //
            // Find the proper serializer based on ApiVersion
            if (apiVersion == null ||
                !_serializers.TryGetValue(apiVersion, out IOuputSerializer<KeyValue> serializer))
            {
                throw new InvalidOperationException("API version not supported.");
            }

            return serializer;
        }

        protected override JsonWriter CreateJsonWriter(TextWriter writer)
        {
            JsonWriter jsonWriter = base.CreateJsonWriter(writer);

            jsonWriter.SetSettings(SerializerSettings);

            return jsonWriter;
        }
    }
}
