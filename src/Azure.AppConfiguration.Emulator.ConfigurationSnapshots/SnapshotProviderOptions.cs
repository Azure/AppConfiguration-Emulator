// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotProviderOptions
    {
        /// <summary>
        /// Maximum result page size
        /// </summary>
        public int OutputPageSize { get; set; } = 100;

        /// <summary>
        /// Total storage read timeout (including retries)
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMilliseconds(6_200);

        /// <summary>
        /// Total storage write timeout (including retries)
        /// </summary>
        public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromMilliseconds(8_300);

        /// <summary>
        /// Timeout between retries for retriable request
        /// </summary>
        public TimeSpan RetryTimeout { get; set; } = TimeSpan.FromMilliseconds(1_500);

        /// <summary>
        /// Timeout between conflict writes
        /// </summary>
        public TimeSpan ConflictRetryTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Minimum filter count
        /// </summary>
        public int MinFilterCount { get; set; }

        /// <summary>
        /// Maximum filter count
        /// </summary>
        public int MaxFilterCount { get; set; }
    }
}
