using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    class ContractResolver : DefaultContractResolver
    {
        private readonly IHttpContextAccessor _accessor;

        public ContractResolver(IHttpContextAccessor accessor)
        {
            NamingStrategy = new SnakeCaseNamingStrategy();
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        protected override JsonObjectContract CreateObjectContract(Type type)
        {
            //
            // KeyValue
            if (typeof(KeyValue) == type)
            {
                return base.CreateObjectContract(typeof(KeyValueModel));
            }

            return base.CreateObjectContract(type);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            RequireApiVersionAttribute versionAttribute = (RequireApiVersionAttribute)member.GetCustomAttribute(typeof(RequireApiVersionAttribute));

            if (versionAttribute != null)
            {
                property.ShouldSerialize =
                    instance => IsEnabled(versionAttribute);

                property.ShouldDeserialize =
                    instance => IsEnabled(versionAttribute);
            }

            return property;
        }

        private bool IsEnabled(RequireApiVersionAttribute versionAttribute)
        {
            ApiVersion apiVersion = _accessor.HttpContext?.GetRequestedApiVersion();

            return apiVersion >= versionAttribute.MinApiVersion;
        }
    }
}
