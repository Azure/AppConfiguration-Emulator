using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service.Utils
{
    sealed class Fields
    {
        private SortedSet<string> _fields;
        private bool _allFields;

        public static readonly Fields All = new Fields("*");
        public static readonly Fields Empty = new Fields(string.Empty);

        public bool HasFields { get; private set; }

        public Fields(params string[] fields)
        {
            if (fields == null)
            {
                _allFields = true;
                return;
            }

            _fields = new SortedSet<string>();

            // Never leave out id
            _fields.Add("id");

            foreach (string s in fields)
            {
                Add(s);
            }
        }

        private Fields()
        {
            _fields = new SortedSet<string>();
            _fields.Add("id");
        }

        public bool Exists(string field)
        {
            return _allFields || _fields.Any(f => f.Equals(field, StringComparison.OrdinalIgnoreCase) || f.StartsWith($"{field}.", StringComparison.OrdinalIgnoreCase));
        }

        public Fields Filter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (_fields == null)
            {
                return Empty;
            }

            filter = filter + ".";

            var newFields = new Fields();

            foreach (var field in _fields)
            {
                if (field.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    newFields.Add(field.Substring(filter.Length));
                }
            }

            return newFields;
        }

        private void Add(string field)
        {
            field = field.Trim();

            if (field == string.Empty)
            {
                return;
            }

            HasFields = true;

            if (field == "*")
            {
                _allFields = true;
            }

            _fields.Add(field);
        }
    }
}
