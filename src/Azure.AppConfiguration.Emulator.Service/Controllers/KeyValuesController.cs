using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Http;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzPolicies = Microsoft.AppConfig.Service.Authorization.Policies;
using TagsAttribute = Azure.AppConfiguration.Emulator.Service.Validators.TagsAttribute;

namespace Azure.AppConfiguration.Emulator.Service
{
    [ApiVersion(ApiVersions.V1)]
    [ApiVersion(ApiVersions.V22_11_preview)]
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09_preview)]
    [ApiController]
    [Route("kv")]
    [Authorize]
    [ValidateActionParameters]
    public class KeyValuesController : Controller
    {
        private readonly IKeyValueProvider _provider;

        public KeyValuesController(IKeyValueProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        [Authorize(AzPolicies.KeyValueRead)]
        [HttpGet]
        [HttpHead]
        [TimeGateFilter]
        [PaginationFilter]
        [AuthorizationScope(ResourceType = ResourceType.Kv)]
        [AllowVersionedParameter(name: "tags", minApiVersion: ApiVersions.V23_11)]
        public async Task<IEnumerable<KeyValue>> Get(
            [FromQuery]
            [Name("key")]
            string key,
            [FromQuery]
            [Name("label")]
            string label,
            [Tags]
            [Name("tags")]
            IEnumerable<KeyValuePair<string, string>> tags,
            string after,
            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,
            CancellationToken cancellationToken)
        {
            return await _provider.Get(
                new KeyValueSearchOptions
                {
                    Key = key,
                    Label = label,
                    Tags = tags,
                    ContinuationToken = after,
                    TimeGate = timeGate
                },
                cancellationToken);
        }

        [Authorize(AzPolicies.KeyValueRead)]
        [HttpGet("{*key}")]
        [HttpHead("{*key}")]
        [IfNull(StatusCodes.Status404NotFound)]
        [TimeGateFilter]
        [AuthorizationScope(ResourceType = ResourceType.Kv)]
        [AllowVersionedParameter(name: "tags", minApiVersion: ApiVersions.V24_09_preview)]
        public async Task<KeyValue> GetKeyValue(
            [Required]
            [Name("key")]
            string key,

            [FromQuery]
            [Name("label")]
            string label,

            [Tags]
            [Name("tags")]
            IEnumerable<KeyValuePair<string, string>> tags,

            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,

            CancellationToken cancellationToken)
        {
            //
            // Escape the filters to ensure exact match criteria
            //
            return (await _provider.Get(
                new KeyValueSearchOptions
                {
                    Key = SearchQuery.Escape(key),
                    Label = SearchQuery.Escape(label) ?? SearchQuery.NullString, // If omitted, consider Null label
                    Tags = tags,
                    TimeGate = timeGate
                },
                cancellationToken))
                .FirstOrDefault();
        }

        [Authorize(AzPolicies.KeyValueWrite)]
        [HttpPut("{*key}")]
        [KeyLockedFilter]
        public async Task<KeyValue> Put(
            [Required]
            [MaxLength(DataModelConstraints.MaxKeyLength)]
            [Literal]
            string key,

            [FromQuery]
            [MaxLength(DataModelConstraints.MaxLabelLength)]
            [Literal(NormalizeNull = true)]
            string label,

            [FromBody]
            [Required]
            KeyValue kv,

            CancellationToken cancellationToken)
        {
            KeyValue existing = await _provider.Get(
                key,
                label,
                cancellationToken);

            EnsurePrecondition(existing);

            kv.Etag = existing?.Etag;
            kv.Label = label;

            await _provider.Set(kv, cancellationToken);

            return kv;
        }

        [Authorize(AzPolicies.KeyValueDelete)]
        [HttpDelete("{*key}")]
        [IfNull(StatusCodes.Status204NoContent)]
        [KeyLockedFilter]
        public async Task<KeyValue> Delete(
            [Required]
            string key,

            [Literal(NormalizeNull = true)]
            [FromQuery]
            string label,

            CancellationToken cancellationToken)
        {
            KeyValue existing = await _provider.Get(
                key,
                label,
                cancellationToken);

            EnsurePrecondition(existing);

            if (existing != null)
            {
                existing.Etag = existing.Etag;

                await _provider.Remove(existing, cancellationToken);
            }

            return existing;
        }

        [HttpPut]
        [HttpPatch("{*key}")]
        [HttpPost("{*key}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        }

        private void EnsurePrecondition(KeyValue kv)
        {
            EtagMatch etagMatch = Request.GetEtagMatch();

            string Etag = Request.GetEtag();

            if (KvHelper.CheckPrecondition(kv, etagMatch, Etag))
            {
                throw new MatchFailedException();
            }
        }
    }
}
