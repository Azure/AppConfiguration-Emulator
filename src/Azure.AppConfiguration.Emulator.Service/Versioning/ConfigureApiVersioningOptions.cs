using Azure.AppConfiguration.Emulator.Service.Formatters.Json;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class ConfigureApiVersioningOptions : IConfigureOptions<ApiVersioningOptions>
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly VersioningOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureApiVersioningOptions(
            IOptions<VersioningOptions> options,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
            IHttpContextAccessor httpContextAccessor)
        {

            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serializerSettings = jsonOptions?.Value?.SerializerSettings ?? throw new ArgumentNullException(nameof(jsonOptions));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Configure(ApiVersioningOptions o)
        {
            _serializerSettings.ContractResolver = new ContractResolver(_httpContextAccessor);

            if (ApiVersion.TryParse(_options.DefaultApiVersion, out ApiVersion version))
            {
                o.DefaultApiVersion = version;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ApiVersionSelector = new DefaultIfUnspecifiedVersionSelector(o);
            }

            o.ErrorResponses = new UnsupportedApiVersionResponseProvider();
        }
    }
}
