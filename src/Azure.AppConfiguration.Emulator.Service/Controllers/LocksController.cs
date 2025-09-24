// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Http;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
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
    [Route("locks")]
    [Authorize(AzPolicies.KeyValueWrite)]
    public class LocksController : Controller
    {
        private readonly IKeyValueProvider _provider;

        public LocksController(IKeyValueProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        [HttpPut("{*key}")]
        [IfNull(StatusCodes.Status404NotFound)]
        public async Task<KeyValue> Put(
            [Required]
            string key,

            [FromQuery]
            [Literal(NormalizeNull = true)]
            string label,

            CancellationToken cancellationToken)
        {
            KeyValue existing = await _provider.GetKeyValue(
                  key,
                  label,
                  cancellationToken);

            EnsurePrecondition(existing);

            if (existing != null &&
                !existing.Locked)
            {
                existing = await _provider.Lock(existing, cancellationToken);
            }

            return existing;
        }

        [HttpDelete("{*key}")]
        [IfNull(StatusCodes.Status404NotFound)]
        public async Task<KeyValue> Delete(
            [Required]
            string key,

            [FromQuery]
            [Literal(NormalizeNull = true)]
            string label,

            CancellationToken cancellationToken)
        {
            KeyValue existing = await _provider.GetKeyValue(
                key,
                label,
                cancellationToken);

            EnsurePrecondition(existing);

            if (existing != null &&
                existing.Locked)
            {
                existing = await _provider.Unlock(existing, cancellationToken);
            }

            return existing;
        }

        [HttpGet("{*key}")]
        [HttpHead("{*key}")]
        [HttpPut]
        [HttpPost("{*key}")]
        [HttpPatch("{*key}")]
        [HttpDelete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        }

        private void EnsurePrecondition(KeyValue kv)
        {
            EtagMatch etagMatch = Request.GetEtagMatch();

            string Etag = Request.GetEtag();

            if (!KvHelper.CheckPrecondition(kv, etagMatch, Etag))
            {
                throw new MatchFailedException();
            }
        }
    }
}
