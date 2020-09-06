using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    public sealed class DnsCompositeAndFilter : IDnsFilter
    {
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        public DnsCompositeAndFilter(IEnumerable<IDnsFilter> dnsFilters) => _dnsFilters = dnsFilters.ToList();

        public DnsCompositeAndFilter(params IDnsFilter[] dnsFilters) => _dnsFilters = dnsFilters;

        public bool IsPermitted(DnsHeader query) => _dnsFilters.All(x => x.IsPermitted(query));
    }
}
