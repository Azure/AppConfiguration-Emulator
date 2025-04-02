// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Service.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System;
using System.Buffers;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    sealed class SnapshotJsonInputFormatter : NewtonsoftJsonInputFormatter
    {
        public SnapshotJsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            MvcOptions options,
            MvcNewtonsoftJsonOptions jsonOptions)
            : base(logger, serializerSettings, charPool, objectPoolProvider, options, jsonOptions)
        {
            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.SnapshotsApplication);
            SupportedMediaTypes.Insert(0, MediaTypeHeaderValues.SnapshotApplication);
        }

        protected override bool CanReadType(Type type)
        {
            return base.CanReadType(type) &&
                   typeof(SnapshotModel).IsAssignableFrom(type);
        }
    }
}
