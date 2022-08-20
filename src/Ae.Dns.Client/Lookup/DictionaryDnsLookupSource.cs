using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Provides a <see cref="IDnsLookupSource"/> implementation based on dictionaries.
    /// </summary>
    public sealed class DictionaryDnsLookupSource : IDnsLookupSource
    {
        private readonly IDictionary<string, IPAddress> _hostsToAddresses = new Dictionary<string, IPAddress>();
        private readonly IDictionary<IPAddress, string> _addressesToHosts = new Dictionary<IPAddress, string>();

        /// <summary>
        /// Construct a new <see cref="DictionaryDnsLookupSource"/> using the specified enumerable of hostnames and IP addresses.
        /// </summary>
        public DictionaryDnsLookupSource(IEnumerable<KeyValuePair<string, IPAddress>> lookup)
        {
            foreach (var kvp in lookup)
            {
                _hostsToAddresses[kvp.Key] = kvp.Value;
                _addressesToHosts[kvp.Value] = kvp.Key;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool TryForwardLookup(string hostname, out IPAddress address) => _hostsToAddresses.TryGetValue(hostname, out address);

        /// <inheritdoc/>
        public bool TryReverseLookup(IPAddress address, out string hostname) => _addressesToHosts.TryGetValue(address, out hostname);
    }
}
