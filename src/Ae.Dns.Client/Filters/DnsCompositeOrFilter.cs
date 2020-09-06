using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    public sealed class DnsCompositeOrFilter : IDnsFilter
    {
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        public DnsCompositeOrFilter(IEnumerable<IDnsFilter> dnsFilters) => _dnsFilters = dnsFilters.ToList();

        public DnsCompositeOrFilter(params IDnsFilter[] dnsFilters) => _dnsFilters = dnsFilters;

        public bool IsPermitted(DnsHeader query) => !_dnsFilters.Any() || _dnsFilters.Any(x => x.IsPermitted(query));
    }
}
