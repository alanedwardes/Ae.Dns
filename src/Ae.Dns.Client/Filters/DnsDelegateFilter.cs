using System;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    public sealed class DnsDelegateFilter : IDnsFilter
    {
        private readonly Func<DnsHeader, bool> _shouldAllow;

        public DnsDelegateFilter(Func<DnsHeader, bool> shouldAllow) => _shouldAllow = shouldAllow;

        public bool IsPermitted(DnsHeader query) => _shouldAllow(query);
    }
}
