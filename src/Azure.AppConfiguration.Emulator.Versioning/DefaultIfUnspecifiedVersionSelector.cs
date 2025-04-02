using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;

namespace Azure.AppConfiguration.Emulator.Versioning
{
    /// <summary>
    /// Use the DefaultApiVersion only if the client hasn't requested any version.
    /// Version selector triggers when api version can't be resolved
    /// </summary>
    public class DefaultIfUnspecifiedVersionSelector : IApiVersionSelector
    {
        private readonly ApiVersioningOptions _options;

        public DefaultIfUnspecifiedVersionSelector(ApiVersioningOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ApiVersion SelectVersion(HttpRequest request, ApiVersionModel model)
        {
            bool useDefaultVersion = _options.AssumeDefaultVersionWhenUnspecified &&
                                     !request.Query.ContainsKey("api-version");

            return useDefaultVersion ? _options.DefaultApiVersion : null;
        }
    }
}
