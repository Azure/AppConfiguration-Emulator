// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValue : ICloneable
    {
        /// <summary>
        /// Key value id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Etag of key value. 
        /// </summary>
        public string Etag { get; set; }

        /// <summary>
        /// Key of key value.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Label of key value.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Content type of key value.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Value of key value. 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Created time of key value.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Tags associated with key value.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Represents wether a key value is locked or not by customer.
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// Time when key value is deleted.
        /// </summary>
        public DateTimeOffset? Deleted { get; set; }

        /// <summary>
        /// Time to live for the key value, after key value is considered as a revision.
        /// </summary>
        public TimeSpan RevisionTTL { get; set; }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
