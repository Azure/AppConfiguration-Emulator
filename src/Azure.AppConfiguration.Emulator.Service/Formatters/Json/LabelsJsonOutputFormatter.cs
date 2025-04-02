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
    sealed class LabelsJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        private readonly IReadOnlyDictionary<ApiVersion, IOuputSerializer<IEnumerable<Label>>> _serializers;

        public LabelsJsonOutputFormatter(
            JsonSerializerSettings settings,
            ArrayPool<char> charPool,
            MvcOptions mvcOptions,
            params IOuputSerializer<IEnumerable<Label>>[] serializers)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
            _serializers = serializers.MapApiVersionAttribute();

            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.LabelsApplication);
        }

        protected override bool CanWriteType(Type type)
        {
            //
            // Supported types 
            if (typeof(IEnumerable<Label>).IsAssignableFrom(type))
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

            if (ctx.Object == null ||
                HttpMethods.IsHead(ctx.HttpContext.Request.Method))
            {
                return;
            }

            //
            // Get serializer
            IOuputSerializer<IEnumerable<Label>> serializer = GetSerializer(ctx.HttpContext);

            LabelFields fields = ctx.HttpContext.Request.GetLabelFields();

            using (var writer = ctx.WriterFactory(ctx.HttpContext.Response.Body, selectedEncoding))
            using (JsonWriter jw = CreateJsonWriter(writer))
            {
                await serializer.WriteContent(jw, (IEnumerable<Label>)ctx.Object, (long)fields);

                // Explitictly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        private IOuputSerializer<IEnumerable<Label>> GetSerializer(HttpContext context)
        {
            Debug.Assert(context != null);

            //
            // Get ApiVersion
            ApiVersion apiVersion = context.GetRequestedApiVersion();

            //
            // Find the proper serializer based on ApiVersion
            if (apiVersion == null ||
                !_serializers.TryGetValue(apiVersion, out IOuputSerializer<IEnumerable<Label>> serializer))
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
