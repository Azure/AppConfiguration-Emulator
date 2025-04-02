using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    sealed class ErrorJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public ErrorJsonOutputFormatter(JsonSerializerSettings settings, ArrayPool<char> charPool, MvcOptions mvcOptions)
            : base(settings, charPool, mvcOptions, jsonOptions: default)
        {
            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.ProblemApplication);
            SupportedEncodings.Add(Encoding.UTF8);
        }

        //
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            //
            // Handle only errors
            if (context.HttpContext.Response.StatusCode >= 400)
            {
                return base.CanWriteResult(context);
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext ctx, Encoding encoding)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (ctx.Object == null ||
                HttpMethods.IsHead(ctx.HttpContext.Request.Method))
            {
                return;
            }

            using (var writer = ctx.WriterFactory(ctx.HttpContext.Response.Body, encoding))
            {
                Write(writer, ctx.Object);

                // Explicitly use AsyncFlush to do async write. Otherwise it will be sync
                await writer.FlushAsync();
            }
        }

        private void Write(TextWriter writer, object error)
        {
            CreateJsonSerializer().Serialize(writer, error);
        }
    }
}
