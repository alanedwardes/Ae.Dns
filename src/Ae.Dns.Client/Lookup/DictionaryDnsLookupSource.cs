using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Provides a <see cref="IDnsLookupSource"/> implementation based on dictionaries.
    /// </summary>
    public sealed class DictionaryDnsLookupSource : IDnsLookupSource
    {
        private readonly IDictionary<string, IList<IPAddress>> _hostsToAddresses = new Dictionary<string, IList<IPAddress>>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<IPAddress, IList<string>> _addressesToHosts = new Dictionary<IPAddress, IList<string>>();

        /// <summary>
        /// Construct a new <see cref="DictionaryDnsLookupSource"/> using the specified enumerable of hostnames and IP addresses.
        /// </summary>
        public DictionaryDnsLookupSource(IEnumerable<KeyValuePair<string, IPAddress>> lookup)
        {
            foreach (var kvp in lookup)
            {
                if (_hostsToAddresses.ContainsKey(kvp.Key))
                {
                    _hostsToAddresses[kvp.Key].Add(kvp.Value);
                }
                else
                {
                    _hostsToAddresses[kvp.Key] = new List<IPAddress> { kvp.Value };
                }

                if (_addressesToHosts.ContainsKey(kvp.Value))
                {
                    _addressesToHosts[kvp.Value].Add(kvp.Key);
                }
                else
                {
                    _addressesToHosts[kvp.Value] = new List<string> { kvp.Key };
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool TryForwardLookup(string hostname, out IList<IPAddress> addresses) => _hostsToAddresses.TryGetValue(hostname, out addresses);

        /// <inheritdoc/>
        public bool TryReverseLookup(IPAddress address, out IList<string> hostnames) => _addressesToHosts.TryGetValue(address, out hostnames);

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DictionaryDnsLookupSource)}";
    }
}
