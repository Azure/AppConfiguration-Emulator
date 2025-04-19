// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Service.Http;
using Azure.AppConfiguration.Emulator.Service.LongRunningOperation;
using Azure.AppConfiguration.Emulator.Service.Utils;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Tenant;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Page = Azure.AppConfiguration.Emulator.ConfigurationSettings.Page<Azure.AppConfiguration.Emulator.ConfigurationSettings.KeyValue>;

namespace Azure.AppConfiguration.Emulator.Service
{
    [ApiVersion(ApiVersions.V22_11_preview)]
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09_preview)]
    [ApiController]
    [Authorize]
    [ValidateActionParameters]
    public class SnapshotsController : Controller
    {
        private readonly ISnapshotProvider _provider;
        private readonly TenantOptions _tenant;

        public SnapshotsController(
            ISnapshotProvider provider,
            IOptions<TenantOptions> tenant)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
        }

        [Authorize(Policies.SnapshotRead)]
        [HttpGet("snapshots")]
        [HttpHead("snapshots")]
        [PaginationFilter]
        [AuthorizationScope(ResourceType = ResourceType.Snapshot)]
        public Task<IEnumerable<Snapshot>> Get(
            [FromQuery]
            string name,
            [ModelBinder(binderType: typeof(SnapshotStatusSearchBinder))]
            SnapshotStatusSearch status,
            string after,
            CancellationToken cancellationToken)
        {
            return _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = name,
                    Status = status,
                    ContinuationToken = after
                },
                cancellationToken);
        }

        [Authorize(Policies.SnapshotRead)]
        [HttpGet("snapshots/{*name}")]
        [HttpHead("snapshots/{*name}")]
        [IfNull(StatusCodes.Status404NotFound)]
        [AuthorizationScope(ResourceType = ResourceType.Snapshot)]
        public async Task<Snapshot> GetSnapshot(
            [Required]
            [FromRoute]
            string name,
            CancellationToken cancellationToken)
        {
            return (await _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = SearchQuery.Escape(name),
                    Status = SnapshotStatusSearch.All
                },
                cancellationToken)).FirstOrDefault();
        }

        [Authorize(Policies.SnapshotRead)]
        [HttpGet("kv")]
        [HttpHead("kv")]
        [PaginationFilter]
        [RouteQuery("snapshot")]
        [AuthorizationScope(ResourceType = ResourceType.Snapshot)]
        public async Task<IEnumerable<KeyValue>> GetContent(
            [FromQuery(Name = "snapshot")]
            string snapshotName,
            string after,
            CancellationToken cancellationToken)
        {
            Snapshot target = (await _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = SearchQuery.Escape(snapshotName),
                    Status = SnapshotStatusSearch.Ready |
                        SnapshotStatusSearch.Archived
                },
                cancellationToken)).FirstOrDefault();

            if (target == null)
            {
                var emptySet = new Page(Enumerable.Empty<KeyValue>());

                emptySet.Etag = KvHelper.ComputeEtag(emptySet);

                return emptySet;
            }

            return await _provider.GetContent(
                target,
                new SnapshotContentSearchOptions
                {
                    ContinuationToken = after
                },
                cancellationToken);
        }

        [Authorize(Policies.SnapshotCreate)]
        [HttpPut("snapshots/{*name}")]
        public async Task<IActionResult> Create(
            [Required]
            [MaxLength(DataModelConstraints.MaxKeyLength)]
            [Literal]
            string name,

            [FromBody]
            [Required]
            SnapshotModel entity,

            CancellationToken cancellationToken)
        {
            Snapshot existing = (await _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = SearchQuery.Escape(name),
                    Status = SnapshotStatusSearch.All
                },
                cancellationToken)).FirstOrDefault();

            if (existing != null)
            {
                return new ObjectResult(Problems.AlreadyExists);
            }

            var snapshot = new Snapshot
            {
                Name = name,
                Tags = entity.Tags,
                CompositionType = entity.CompositionType.HasValue ? entity.CompositionType.Value : CompositionType.Key,
                RetentionPeriod = entity.RetentionPeriod.HasValue
                    ? TimeSpan.FromSeconds(entity.RetentionPeriod.Value)
                    : _tenant.ConfigurationSnapshotDefaultRetentionPeriod
            };

            // KeyValueFilters
            if (entity.Filters != null)
            {
                snapshot.Filters = entity.Filters.Select(f =>
                    {
                        List<KeyValuePair<string, string>> tagFilters = null;

                        if (f.Tags != null && f.Tags.Any())
                        {
                            tagFilters = new List<KeyValuePair<string, string>>();

                            foreach (string tag in f.Tags)
                            {
                                if (!string.IsNullOrWhiteSpace(tag))
                                {
                                    tagFilters.Add(SearchQueryHelper.ParseTagFilter(tag.AsSpan()));
                                }
                            }
                        }

                        return new KeyValueFilter
                        {
                            Key = f.Key,
                            Label = f.Label,
                            Tags = tagFilters
                        };
                    });
            }

            try
            {
                await _provider.Create(
                    snapshot,
                    cancellationToken);
            }
            catch (ConflictException)
            {
                return new ObjectResult(Problems.AlreadyExists);
            }

            var uri = new UriBuilder
            {
                Scheme = Request.Scheme,
                Host = Request.Host.Host,
                Path = $"operations?snapshot={Uri.EscapeDataString(snapshot.Name)}&api-version={HttpContext.GetRequestedApiVersion()}"
            };

            if (Request.Host.Port.HasValue)
            {
                uri.Port = Request.Host.Port.Value;
            }

            Response.Headers[HeaderNames.OperationLocation] = uri.ToString();

            return new ObjectResult(snapshot)
            {
                StatusCode = StatusCodes.Status201Created
            };
        }

        [Authorize(Policies.SnapshotArchive)]
        [HttpPatch("snapshots/{*name}")]
        public async Task<IActionResult> Archive(
            [Required]
            string name,

            [FromBody]
            [Required]
            SnapshotUpdateParameters updateParameters,

            CancellationToken cancellationToken)
        {
            if (updateParameters.Status != SnapshotStatus.Archived &&
                updateParameters.Status != SnapshotStatus.Ready)
            {
                throw new ArgumentOutOfRangeException(nameof(updateParameters));
            }

            Snapshot snapshot = (await _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = SearchQuery.Escape(name),
                    Status = SnapshotStatusSearch.All
                },
                cancellationToken)).FirstOrDefault();

            if (snapshot == null)
            {
                return NotFound();
            }

            Request.ValidatePrecondition(snapshot.Etag);

            if (updateParameters.Status == SnapshotStatus.Archived)
            {
                if (snapshot.Status == SnapshotStatus.Archived)
                {
                    return new ObjectResult(snapshot);
                }

                if (snapshot.Status != SnapshotStatus.Ready)
                {
                    return new ObjectResult(Problems.InvalidState);
                }

                try
                {
                    await _provider.Archive(
                        snapshot,
                        cancellationToken);
                }
                catch (SnapshotNotFoundException)
                {
                    return NotFound();
                }
                catch (ConflictException e)
                {
                    throw new TimeoutException("Conflict", e);
                }
            }
            else if (updateParameters.Status == SnapshotStatus.Ready)
            {
                if (snapshot.Status == SnapshotStatus.Ready)
                {
                    return new ObjectResult(snapshot);
                }

                if (snapshot.Status != SnapshotStatus.Archived)
                {
                    return new ObjectResult(Problems.InvalidState);
                }

                try
                {
                    await _provider.Recover(
                        snapshot,
                        cancellationToken);
                }
                catch (SnapshotNotFoundException)
                {
                    return NotFound();
                }
                catch (ConflictException e)
                {
                    throw new TimeoutException("Conflict", e);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return new ObjectResult(snapshot);
        }

        [Authorize(Policies.SnapshotRead)]
        [HttpGet("operations")]
        [OperationStatusFilter]
        public async Task<IActionResult> GetOperationStatus(
            [FromQuery(Name = "snapshot")]
            [Required]
            string snapshotName,

            CancellationToken cancellationToken)
        {
            Snapshot snapshot = (await _provider.Get(
                new SnapshotSearchOptions
                {
                    Name = SearchQuery.Escape(snapshotName),
                    Status = SnapshotStatusSearch.All
                },
                cancellationToken)).FirstOrDefault();

            if (snapshot == null)
            {
                return NotFound();
            }

            return new ObjectResult(snapshot.ToOperationStatus());
        }
    }
}
