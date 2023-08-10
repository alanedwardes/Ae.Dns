﻿using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Collections.Generic;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Provides an <see cref="IDnsFilter"/> implementation which disallows the specified set of <see cref="DnsQueryType"/>.
    /// </summary>
    public sealed class DnsQueryTypeFilter : IDnsFilter
    {
        private readonly ISet<DnsQueryType> _disallowedQueryTypes;

        /// <summary>
        /// Create a new <see cref="DnsQueryTypeFilter"/> with the provided set of <see cref="DnsQueryType"/> to block.
        /// </summary>
        /// <param name="disallowedQueryTypes"></param>
        public DnsQueryTypeFilter(IEnumerable<DnsQueryType> disallowedQueryTypes) => _disallowedQueryTypes = new HashSet<DnsQueryType>(disallowedQueryTypes);

        /// <inheritdoc/>
        public bool IsPermitted(DnsMessage query)
        {
            if (_disallowedQueryTypes.Contains(query.Header.QueryType))
            {
                query.Header.Tags["BlockReason"] = nameof(DnsQueryTypeFilter);
                return false;
            }

            return true;
        }
    }
}
