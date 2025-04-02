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
    sealed class KeysJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        private readonly IReadOnlyDictionary<ApiVersion, IOuputSerializer<IEnumerable<Key>>> _serializers;

        public KeysJsonOutputFormatter(
            JsonSerializerSettings settings,
            ArrayPool<char> charPool,
            MvcOptions mvcOptions,
            params IOuputSerializer<IEnumerable<Key>>[] serializers)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
            _serializers = serializers.MapApiVersionAttribute();

            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.KeysApplication);
        }

        protected override bool CanWriteType(Type type)
        {
            //
            // Supported types 
            if (typeof(IEnumerable<Key>).IsAssignableFrom(type))
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
            IOuputSerializer<IEnumerable<Key>> serializer = GetSerializer(ctx.HttpContext);

            KeyFields fields = ctx.HttpContext.Request.GetKeyFields();

            using (var writer = ctx.WriterFactory(ctx.HttpContext.Response.Body, selectedEncoding))
            using (JsonWriter jw = CreateJsonWriter(writer))
            {
                await serializer.WriteContent(jw, (IEnumerable<Key>)ctx.Object, (long)fields);

                // Explitictly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        private IOuputSerializer<IEnumerable<Key>> GetSerializer(HttpContext context)
        {
            Debug.Assert(context != null);

            //
            // Get ApiVersion
            ApiVersion apiVersion = context.GetRequestedApiVersion();

            //
            // Find the proper serializer based on ApiVersion
            if (apiVersion == null ||
                !_serializers.TryGetValue(apiVersion, out IOuputSerializer<IEnumerable<Key>> serializer))
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
