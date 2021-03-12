using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Represents a DNS filter consisting of a list of other filters ORed together.
    /// For example, if one filter allows the query, it is successful.
    /// </summary>
    public sealed class DnsCompositeOrFilter : IDnsFilter
    {
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        /// <summary>
        /// Create a new composite OR filter using the specified filters.
        /// </summary>
        public DnsCompositeOrFilter(IEnumerable<IDnsFilter> dnsFilters) => _dnsFilters = dnsFilters.ToList();

        /// <summary>
        /// Create a new composite OR filter using the specified filters.
        /// </summary>
        public DnsCompositeOrFilter(params IDnsFilter[] dnsFilters) => _dnsFilters = dnsFilters;

        /// <inheritdoc/>
        public bool IsPermitted(DnsHeader query) => !_dnsFilters.Any() || _dnsFilters.Any(x => x.IsPermitted(query));
    }
}
