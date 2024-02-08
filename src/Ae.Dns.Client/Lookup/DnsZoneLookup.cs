using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Allows forward and reverse address lookups using an <see cref="IDnsZone"/>.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsZoneLookup : IDnsLookupSource
    {
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Construct a <see cref="DnsZoneLookup"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="dnsZone"></param>
        public DnsZoneLookup(IDnsZone dnsZone)
        {
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool TryForwardLookup(string hostname, out IList<IPAddress> addresses)
        {
            addresses = _dnsZone.Records.Where(x => x.Host == hostname)
                .Select(x => x.Resource)
                .OfType<DnsIpAddressResource>()
                .Select(x => x.IPAddress)
                .ToList();

            return addresses.Count > 0;
        }

        /// <inheritdoc/>
        public bool TryReverseLookup(IPAddress address, out IList<string> hostnames)
        {
            hostnames = _dnsZone.Records.Where(x => x.Resource is DnsIpAddressResource ip && ip.IPAddress == address)
                .Select(x => x.Host.ToString())
                .ToList();

            return hostnames.Count > 0;
        }
    }
}
