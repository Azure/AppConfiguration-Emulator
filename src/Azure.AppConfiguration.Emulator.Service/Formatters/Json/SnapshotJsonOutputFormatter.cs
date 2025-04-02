using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Service.Formatters;
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
    sealed class SnapshotJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        private readonly IReadOnlyDictionary<ApiVersion, IOuputSerializer<Snapshot>> _serializers;

        public SnapshotJsonOutputFormatter(
            JsonSerializerSettings settings,
            ArrayPool<char> charPool,
            MvcOptions mvcOptions,
            params IOuputSerializer<Snapshot>[] serializers)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
            _serializers = serializers.MapApiVersionAttribute();

            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.SnapshotApplication);
        }

        protected override bool CanWriteType(Type type)
        {
            //
            // Supported types 
            if (typeof(Snapshot).IsAssignableFrom(type))
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
            IOuputSerializer<Snapshot> serializer = GetSerializer(ctx.HttpContext);

            SnapshotFields fields = request.GetSnapshotFields();

            using (var writer = ctx.WriterFactory(response.Body, selectedEncoding))
            using (JsonWriter jw = CreateJsonWriter(writer))
            {
                await serializer.WriteContent(
                    jw,
                    (Snapshot)ctx.Object,
                    (long)fields);

                // Explicitly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            Snapshot snapshot = (Snapshot)context.Object;

            //
            // Handle conditional request
            if (context.HttpContext.Request.IsRead() &&
                context.HttpContext.Request.TryEvaluatePreconditionStatusCode(snapshot?.Etag, out int statusCode))
            {
                context.HttpContext.Response.StatusCode = statusCode;
                return;
            }

            base.WriteResponseHeaders(context);

            if (snapshot == null)
            {
                return;
            }

            //
            // Get serializer
            IOuputSerializer<Snapshot> serializer = GetSerializer(context.HttpContext);

            //
            // Write headers
            serializer.WriteResponseHeaders(context.HttpContext.Response, snapshot);
        }

        private IOuputSerializer<Snapshot> GetSerializer(HttpContext context)
        {
            Debug.Assert(context != null);

            //
            // Get ApiVersion
            ApiVersion apiVersion = context.GetRequestedApiVersion();

            //
            // Find the proper serializer based on ApiVersion
            if (apiVersion == null ||
                !_serializers.TryGetValue(apiVersion, out IOuputSerializer<Snapshot> serializer))
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
