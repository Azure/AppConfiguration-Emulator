// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service
{
    public static class IEnumerableExtensions
    {
        public static IReadOnlyDictionary<ApiVersion, T> MapApiVersionAttribute<T>(this IEnumerable<T> values)
        {
            var serializers = new Dictionary<ApiVersion, T>();

            foreach (T val in values)
            {
                ApiVersionAttribute[] versionAttributes = val.GetType().GetCustomAttributes(typeof(ApiVersionAttribute), true) as ApiVersionAttribute[];

                if (versionAttributes != null)
                {
                    foreach (ApiVersionAttribute attr in versionAttributes)
                    {
                        ApiVersion version = attr.Versions.FirstOrDefault();

                        if (version != null)
                        {
                            serializers[version] = val;
                        }
                    }
                }
            }

            return serializers;
        }
    }
}
