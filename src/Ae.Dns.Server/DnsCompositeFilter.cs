using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Protocol;

namespace Ae.Dns.Server
{
    public sealed class DnsCompositeFilter : IDnsFilter
    {
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        public DnsCompositeFilter(IEnumerable<IDnsFilter> dnsFilters) => _dnsFilters = dnsFilters.ToList();

        public DnsCompositeFilter(params IDnsFilter[] dnsFilters) => _dnsFilters = dnsFilters;

        public bool IsPermitted(DnsHeader query) => !_dnsFilters.Any() || _dnsFilters.Any(x => x.IsPermitted(query));
    }
}
