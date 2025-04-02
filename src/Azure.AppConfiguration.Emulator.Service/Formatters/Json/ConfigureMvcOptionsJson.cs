// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Service.Formatters.Serializer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Buffers;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    public class ConfigureMvcNewtonsoftJsonOptions : IConfigureOptions<MvcNewtonsoftJsonOptions>
    {
        private readonly IHostEnvironment _hostingEnv;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureMvcNewtonsoftJsonOptions(IHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _hostingEnv = env ?? throw new ArgumentNullException(nameof(env));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Configure(MvcNewtonsoftJsonOptions o)
        {
            if (_hostingEnv.IsDevelopment())
            {
                o.SerializerSettings.Formatting = Formatting.Indented;
            }

            o.SerializerSettings.ContractResolver = new ContractResolver(_httpContextAccessor);

            o.SerializerSettings.Converters.Add(new CompositionTypeConverter());
        }
    }

    public class ConfigureMvcJsonOptions : IConfigureOptions<MvcOptions>
    {
        private readonly MvcNewtonsoftJsonOptions _jsonOptions;
        private readonly ArrayPool<char> _charPool;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ObjectPoolProvider _objectPoolProvider;

        public ConfigureMvcJsonOptions(
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
        {
            _jsonOptions = jsonOptions?.Value ?? throw new ArgumentNullException(nameof(jsonOptions));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));
            _objectPoolProvider = objectPoolProvider ?? throw new ArgumentNullException(nameof(objectPoolProvider));
        }

        public void Configure(MvcOptions o)
        {
            o.InputFormatters.Insert(
                0,
                new KeyValueJsonInputFormatter(
                    _loggerFactory.CreateLogger<KeyValueJsonInputFormatter>(),
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    _objectPoolProvider,
                    o,
                    _jsonOptions));

            o.InputFormatters.Insert(
                1,
                new SnapshotJsonInputFormatter(
                    _loggerFactory.CreateLogger<SnapshotJsonInputFormatter>(),
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    _objectPoolProvider,
                    o,
                    _jsonOptions));

            o.InputFormatters.Insert(
                2,
                new SnapshotUpdateParametersJsonInputFormatter(
                    _loggerFactory.CreateLogger<SnapshotUpdateParametersJsonInputFormatter>(),
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    _objectPoolProvider,
                    o,
                    _jsonOptions));

            o.OutputFormatters.Insert(
                0,
                new KeyValueJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new KvJsonOutputSerializer()));

            o.OutputFormatters.Insert(
                1,
                new KvsJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new KvsJsonOutputSerializer(),
                    new KvsJsonOutputSerializer2()));

            o.OutputFormatters.Insert(
                2,
                new KeysJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new KeysJsonOutputSerializer()));

            o.OutputFormatters.Insert(
                3,
                new ErrorJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o));

            o.OutputFormatters.Insert(
                4,
                new LabelsJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new LabelsJsonOutputSerializer()));

            o.OutputFormatters.Insert(
                5,
                new SnapshotJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new SnapshotJsonOutputSerializer(),
                    new SnapshotJsonOutputSerializer2()));

            o.OutputFormatters.Insert(
                6,
                new SnapshotsJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o,
                    new SnapshotsJsonOutputSerializer(),
                    new SnapshotsJsonOutputSerializer2()));

            o.OutputFormatters.Insert(
                7,
                new OperationStatusJsonOutputFormatter(
                    _jsonOptions.SerializerSettings,
                    _charPool,
                    o));
        }
    }

    public static class JsonFormatterExtensions
    {
        public static IMvcBuilder AddKeyValueJsonFormatter(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ServiceDescriptor descriptor = ServiceDescriptor.Transient<IConfigureOptions<MvcNewtonsoftJsonOptions>, ConfigureMvcNewtonsoftJsonOptions>();
            builder.Services.TryAddEnumerable(descriptor);

            descriptor = ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, ConfigureMvcJsonOptions>();
            builder.Services.TryAddEnumerable(descriptor);

            return builder;
        }
    }
}
