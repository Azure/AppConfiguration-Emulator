// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzPolicies = Microsoft.AppConfig.Service.Authorization.Policies;

namespace Azure.AppConfiguration.Emulator.Service
{
    [ApiVersion(ApiVersions.V1)]
    [ApiVersion(ApiVersions.V22_11_preview)]
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09)]
    [ApiController]
    [Route("keys")]
    [Authorize(AzPolicies.KeyValueRead)]

    public class KeysController : Controller
    {
        private readonly IKeyProvider _provider;

        public KeysController(IKeyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        [HttpGet]
        [HttpHead]
        [TimeGateFilter]
        [PaginationFilter]
        public async Task<IEnumerable<Key>> Get(
            [FromQuery] string name,
            string after,
            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,
            CancellationToken cancellationToken)
        {
            return await _provider.QueryKeys(
                new KeySearchOptions
                {
                    KeyFilter = SearchQuery.CreateStringFilter(name),
                    ContinuationToken = after,
                    TimeGate = timeGate
                },
                cancellationToken)
                .AsTask();
        }

        [HttpPut("{*name}")]
        [HttpPost("{*name}")]
        [HttpPatch("{*name}")]
        [HttpDelete("{*name}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        }
    }
}
