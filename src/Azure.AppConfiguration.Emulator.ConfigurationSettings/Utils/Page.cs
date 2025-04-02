using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class Page<T> : IEnumerable<T>, IPage
    {
        private IEnumerable<T> _items;

        public Page(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public long TotalItemsCount { get; set; }

        public long Offset { get; set; }

        public int Count => _items.Count();

        public string ContinuationToken { get; set; }

        public string NextLink { get; set; }

        public string Etag { get; set; }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}
