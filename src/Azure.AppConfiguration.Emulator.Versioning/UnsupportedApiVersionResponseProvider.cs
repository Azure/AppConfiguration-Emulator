using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace Azure.AppConfiguration.Emulator.Versioning
{
    public class UnsupportedApiVersionResponseProvider : DefaultErrorResponseProvider
    {
        private const string InvalidArgument = "https://azconfig.io/errors/invalid-argument";

        public override IActionResult CreateResponse(ErrorResponseContext context)
        {
            var error = new
            {
                type = InvalidArgument,
                title = CreateTitle(context.ErrorCode),
                name = "api-version",
                detail = context.Message,
                status = context.StatusCode,
            };

            //
            // Set status code
            context.Request.HttpContext.Response.StatusCode = context.StatusCode;

            //
            // Content
            return new ObjectResult(error);
        }

        private static string CreateTitle(string errorCode)
        {
            switch (errorCode)
            {
                case ErrorCodes.ApiVersionUnspecified: return ErrorMessages.ApiVersionUnspecified;
                case ErrorCodes.UnsupportedApiVersion: return ErrorMessages.UnsupportedApiVersion;
                case ErrorCodes.AmbiguousApiVersion: return ErrorMessages.AmbiguousApiVersion;
                case ErrorCodes.ErrorMissingRequiredProperty: return ErrorMessages.ErrorMissingRequiredProperty;

                default: return ErrorMessages.InvalidApiVersion;
            }
        }
    }
}
