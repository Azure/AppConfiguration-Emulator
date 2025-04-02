// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using OperationStatus = Azure.AppConfiguration.Emulator.Service.LongRunningOperation.OperationStatus;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    sealed class OperationStatusJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public OperationStatusJsonOutputFormatter(JsonSerializerSettings settings, ArrayPool<char> charPool, MvcOptions mvcOptions)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
        }

        protected override bool CanWriteType(Type type)
        {
            //
            // Supported types 
            if (typeof(OperationStatus).IsAssignableFrom(type))
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
                HttpMethods.IsHead(request.Method))
            {
                return;
            }

            using (var writer = ctx.WriterFactory(ctx.HttpContext.Response.Body, selectedEncoding))
            {
                await WriteAsync(
                    writer,
                    (OperationStatus)ctx.Object);

                // Explicitly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        private async Task WriteAsync(
            TextWriter writer,
            OperationStatus operationStatus)
        {
            using (JsonWriter jw = CreateJsonWriter(writer))
            {
                await jw.WriteAsync(operationStatus);
            }
        }

        protected override JsonWriter CreateJsonWriter(TextWriter writer)
        {
            JsonWriter jsonWriter = base.CreateJsonWriter(writer);

            jsonWriter.SetSettings(SerializerSettings);

            return jsonWriter;
        }
    }
}
