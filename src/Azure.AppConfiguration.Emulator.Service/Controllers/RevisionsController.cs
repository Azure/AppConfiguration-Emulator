// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzPolicies = Microsoft.AppConfig.Service.Authorization.Policies;
using TagsAttribute = Azure.AppConfiguration.Emulator.Service.Validators.TagsAttribute;

namespace Azure.AppConfiguration.Emulator.Service
{
    [ApiVersion(ApiVersions.V1)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09)]
    [ApiController]
    [Route("revisions")]
    [Authorize(AzPolicies.KeyValueRead)]
    [ValidateActionParameters]
    public class RevisionsController : Controller
    {
        private readonly IRevisionProvider _provider;

        public RevisionsController(IRevisionProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        [HttpGet]
        [HttpHead]
        [TimeGateFilter]
        [PaginationFilter]
        [AllowVersionedParameter(name: "tags", minApiVersion: ApiVersions.V23_11)]
        public async Task<IEnumerable<KeyValue>> Get(
            [FromQuery] string key,
            [FromQuery] string label,

            [Tags]
            IEnumerable<KeyValuePair<string, string>> tags,

            [FromQuery]
            string after,

            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,
            CancellationToken cancellationToken)
        {
            return await _provider.QueryRevisions(
                new KeyValueSearchOptions
                {
                    KeyFilter = SearchQuery.CreateStringFilter(key),
                    LabelFilter = SearchQuery.CreateStringFilter(label),
                    Tags = tags,
                    ContinuationToken = after,
                    TimeGate = timeGate
                },
                cancellationToken);
        }

        [HttpPut("{*key}")]
        [HttpPost("{*key}")]
        [HttpPatch("{*key}")]
        [HttpDelete("{*key}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        }
    }
}
