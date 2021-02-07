using Ae.Dns.Protocol;
using System.Collections.Generic;

namespace Ae.Dns.Client.Filters
{
    public sealed class DnsDomainSuffixFilter : IDnsFilter
    {
        private readonly List<string> _domainSuffixes = new List<string>();

        public DnsDomainSuffixFilter(params string[] suffixes) => AddSuffixFilters(suffixes);

        public void AddSuffixFilters(params string[] suffixes) => _domainSuffixes.AddRange(suffixes);

        public bool IsPermitted(DnsHeader query)
        {
            foreach (var suffix in _domainSuffixes)
            {
                if (query.Host.EndsWith(suffix))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
