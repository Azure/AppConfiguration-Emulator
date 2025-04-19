// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    static class PartialEnumerableExtentions
    {
        public static Page<T> TakeRange<T>(this IEnumerable<T> source, Range range)
        {
            int offset = 0;
            int totalItems = source.Count();
            int take = totalItems;

            //
            // Check range
            offset = range.Start.Value;

            if (totalItems <= offset)
            {
                throw new RangeFailedException("range");
            }

            if (range.End.Value < int.MaxValue)
            {
                take = range.End.Value - offset + 1;

                if (totalItems < offset + take)
                {
                    throw new RangeFailedException("range");
                }
            }

            IEnumerable<T> result = source.Skip(offset).Take(take);

            return new Page<T>(result)
            {
                TotalItemsCount = source.Count(),
                Offset = range.Start.Value
            };
        }
    }
}
