// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    /// <summary>
    /// Snapshot type used for MVC binding and validation
    /// </summary>
    public class SnapshotModel : IValidatableObject
    {
        private const int MinimumFilterCount = 1;

        private const int MaximumFilterCount = 3;

        private static readonly int MinimumRetentionPeriod = (int)TimeSpan.FromHours(1).TotalSeconds;

        public CompositionType? CompositionType { get; set; }

        public IEnumerable<KeyValueFilterModel> Filters { get; set; }

        public int? RetentionPeriod { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            TenantOptions tenant = validationContext.GetRequiredService<IOptions<TenantOptions>>().Value;

            if (tenant == null)
            {
                throw new InvalidOperationException("Tenant is required.");
            }

            return new ValidationResult[]
            {
                Validate(tenant)
            };
        }

        private ValidationResult Validate(TenantOptions tenant)
        {
            Debug.Assert(tenant != null);

            //
            // filters
            int count = Filters != null ? Filters.Count() : 0;

            if (count < MinimumFilterCount ||
                count > MaximumFilterCount)
            {
                return new ValidationResult(
                    string.Empty,
                    new string[]
                    {
                        "filters"
                    });
            }

            int index = 0;

            foreach (KeyValueFilterModel filter in Filters)
            {
                //
                // key
                if (filter.Key == null ||
                    filter.Key.Length > DataModelConstraints.MaxKeyLength ||
                    filter.Key.Any(char.IsControl))
                {
                    return new ValidationResult(
                        string.Empty,
                        new string[]
                        {
                            $"filters[{index}].key"
                        });
                }

                //
                // label
                string label = SearchQuery.NormalizeNull(filter.Label);

                if (label != null &&
                    (label.Length > DataModelConstraints.MaxLabelLength ||
                        label.Any(char.IsControl)))
                {
                    return new ValidationResult(
                        string.Empty,
                        new string[]
                        {
                            $"filters[{index}].label"
                        });
                }

                //
                // Tags
                if (filter.Tags != null)
                {
                    foreach (string tagFilter in filter.Tags)
                    {
                        if (tagFilter == null ||
                            tagFilter.Length > DataModelConstraints.MaxTagNameLength + DataModelConstraints.MaxTagValueLength + 1)
                        {
                            return new ValidationResult(
                                string.Empty,
                                new string[]
                                {
                                    $"filters[{index}].tags"
                                });
                        }
                    }
                }

                index++;
            }

            //
            // tags
            if (Tags != null)
            {
                ValidationResult tagsValidationResult = ValidateTags(Tags);

                if (tagsValidationResult != ValidationResult.Success)
                {
                    return tagsValidationResult;
                }
            }

            //
            // retention_period
            if (RetentionPeriod.HasValue &&
                (RetentionPeriod.Value < MinimumRetentionPeriod ||
                    RetentionPeriod.Value > (int)tenant.ConfigurationSnapshotMaxRetentionPeriod.TotalSeconds))
            {
                return new ValidationResult(
                    string.Empty,
                    new string[]
                    {
                        "retention_period"
                    });
            }

            return ValidationResult.Success;
        }

        private static ValidationResult ValidateTags(IDictionary<string, string> tags)
        {
            Debug.Assert(tags != null);

            foreach (KeyValuePair<string, string> tag in tags)
            {
                //
                // key
                if (tag.Key == null)
                {
                    return new ValidationResult(
                        string.Empty,
                        new string[]
                        {
                            "tags[].key"
                        });
                }

                if (tag.Key.Length > DataModelConstraints.MaxTagNameLength ||
                    tag.Key.Any(char.IsControl))
                {
                    return new ValidationResult(
                        string.Empty,
                        new string[]
                        {
                            $"tags[{tag.Key}].key"
                        });
                }

                //
                // value
                if (tag.Value != null &&
                    (tag.Value.Length > DataModelConstraints.MaxTagValueLength ||
                        tag.Value.Any(char.IsControl)))
                {
                    return new ValidationResult(
                        string.Empty,
                        new string[]
                        {
                            $"tags[{tag.Key}].value"
                        });
                }
            }

            return ValidationResult.Success;
        }
    }
}
