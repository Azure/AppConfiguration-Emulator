// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Tenant
{
    public class TenantOptions
    {
        public string EntraTenantId { get; init; }

        public string ResourceName { get; init; }

        public string SubscriptionId { get; init; }

        public string ResourceGroup { get; init; }

        public string ResourceId { get; init; }

        public IEnumerable<AccessKey> AccessKeys { get; init; }

        public bool EntraIdAuthenticationEnabled { get; init; }

        public bool HmacSha256Enabled { get; init; }

        public bool AnonymousAuthEnabled { get; init; }

        public TimeSpan ConfigurationSnapshotDefaultRetentionPeriod { get; init; } = TimeSpan.FromDays(2);

        public TimeSpan ConfigurationSnapshotMaxRetentionPeriod { get; init; } = TimeSpan.FromDays(7);

        public TimeSpan ConfigurationSettingRetentionPeriod { get; init; } = TimeSpan.FromDays(7);

        public int OutputPageSize { get; init; } = 100;
    }
}
