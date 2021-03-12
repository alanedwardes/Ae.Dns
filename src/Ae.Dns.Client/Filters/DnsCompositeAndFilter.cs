using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Represents a DNS filter consisting of a list of other filters ANDed together.
    /// For example, all filters must allow the query for it to be successful.
    /// </summary>
    public sealed class DnsCompositeAndFilter : IDnsFilter
    {
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        /// <summary>
        /// Create a new composite AND filter using the specified filters.
        /// </summary>
        public DnsCompositeAndFilter(IEnumerable<IDnsFilter> dnsFilters) => _dnsFilters = dnsFilters.ToList();

        /// <summary>
        /// Create a new composite AND filter using the specified filters.
        /// </summary>
        public DnsCompositeAndFilter(params IDnsFilter[] dnsFilters) => _dnsFilters = dnsFilters;

        /// <inheritdoc/>
        public bool IsPermitted(DnsHeader query) => _dnsFilters.All(x => x.IsPermitted(query));
    }
}
