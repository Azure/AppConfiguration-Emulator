// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    sealed class KeyValueJsonInputFormatter : NewtonsoftJsonInputFormatter
    {
        public KeyValueJsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            MvcOptions options,
            MvcNewtonsoftJsonOptions jsonOptions)
            : base(logger, serializerSettings, charPool, objectPoolProvider, options, jsonOptions)
        {
            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.KvsApplication);
            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.KeyValueApplication);
        }

        protected override bool CanReadType(Type type)
        {
            return base.CanReadType(type) &&
                   (typeof(KeyValue).IsAssignableFrom(type) ||
                    typeof(IEnumerable<KeyValue>).IsAssignableFrom(type));
        }
    }
}
