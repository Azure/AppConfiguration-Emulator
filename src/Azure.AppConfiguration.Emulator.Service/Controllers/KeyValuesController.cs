// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09)]
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
            [FromQuery(Name = "key")]
            string keyFilter,
            [FromQuery(Name ="label")]
            string labelFilter,
            [Tags]
            IEnumerable<KeyValuePair<string, string>> tags,
            string after,
            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,
            CancellationToken cancellationToken)
        {
            return await _provider.QueryKeyValues(
                new KeyValueSearchOptions
                {
                    KeyFilter = SearchQuery.CreateStringFilter(keyFilter),
                    LabelFilter = SearchQuery.CreateStringFilter(labelFilter),
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
        [AllowVersionedParameter(name: "tags", minApiVersion: ApiVersions.V24_09)]
        public async Task<KeyValue> GetKeyValue(
            [Required]
            string key,

            [FromQuery]
            [Literal(NormalizeNull = true)]
            string label,

            [Tags]
            IEnumerable<KeyValuePair<string, string>> tags,

            [IgnoreBinding(nameof(TimeGateFilter))] DateTimeOffset? timeGate,

            CancellationToken cancellationToken)
        {
            //
            // Escape the filters to ensure exact match criteria
            return (await _provider.QueryKeyValues(
                new KeyValueSearchOptions
                {
                    KeyFilter = new StringFilter
                    {
                        EqualsTo = SearchQuery.Escape(key)
                    },
                    LabelFilter = SearchQuery.IsNullOrZero(label) ?
                        StringFilter.NullString :
                        new StringFilter
                        {
                            EqualsTo = SearchQuery.Escape(label)
                        },
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
            KeyValueModel model,

            CancellationToken cancellationToken)
        {
            KeyValue existing = await _provider.GetKeyValue(
                key,
                label,
                cancellationToken);

            EnsurePrecondition(existing);

            var kv = new KeyValue
            {
                Key = key,
                Label = SearchQuery.NormalizeNull(label),
                ContentType = model.ContentType,
                Value = model.Value,
                Tags = model.Tags?.AsReadOnly()
            };

            //
            // No-op if equivalent
            if (KvEquivalent(kv, existing))
            {
                return existing;
            }

            kv.Etag = existing?.Etag;

            return await _provider.Set(kv, cancellationToken);
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
            KeyValue existing = await _provider.GetKeyValue(
                key,
                label,
                cancellationToken);

            EnsurePrecondition(existing);

            if (existing != null &&
                existing.Deleted == null)
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
            //
            // Check conditional etag
            EtagMatch etagMatch = Request.GetEtagMatch();

            string Etag = Request.GetEtag();

            if (!KvHelper.CheckPrecondition(kv, etagMatch, Etag))
            {
                throw new MatchFailedException();
            }

            //
            // Check if locked
            if (kv != null && kv.Locked)
            {
                throw new KeyLockedException($"key={kv.Key},label={kv.Label}");
            }
        }

        private bool KvEquivalent(KeyValue x, KeyValue y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            var EmptyTags = Enumerable.Empty<KeyValuePair<string, string>>();

            return x.Key == y.Key &&
                   x.ContentType == y.ContentType &&
                   x.Value == y.Value &&
                   Enumerable.SequenceEqual(
                       x.Tags ?? EmptyTags,
                       y.Tags ?? EmptyTags);
        }
    }
}
