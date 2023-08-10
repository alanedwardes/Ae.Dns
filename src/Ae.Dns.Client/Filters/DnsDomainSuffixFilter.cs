using Ae.Dns.Protocol;
using System.Collections.Generic;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Provides the ability to use partial suffix matching to deny DNS queries.
    /// </summary>
    public sealed class DnsDomainSuffixFilter : IDnsFilter
    {
        private readonly List<string> _domainSuffixes = new List<string>();

        /// <summary>
        /// Create an instance of the suffix filter with the supplied suffixes.
        /// </summary>
        public DnsDomainSuffixFilter(params string[] suffixes) => AddSuffixFilters(suffixes);

        /// <summary>
        /// Add the specified suffix filter to the list of suffixes.
        /// For example, specifying example.com will deny anything ending in example.com
        /// </summary>
        public void AddSuffixFilters(params string[] suffixes) => _domainSuffixes.AddRange(suffixes);

        /// <inheritdoc/>
        public bool IsPermitted(DnsMessage query)
        {
            foreach (var suffix in _domainSuffixes)
            {
                if (query.Header.Host.EndsWith(suffix))
                {
                    query.Header.Tags["BlockReason"] = "Suffix filter";
                    return false;
                }
            }

            return true;
        }
    }
}
