using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Describes a service capable of resolving hostnames to IP addreses, and IP addresses to hostnames.
    /// </summary>
    public interface IDnsLookupSource : IDisposable
    {
        /// <summary>
        /// Try to resolve the specified hostname to an <see cref="IPAddress"/>.
        /// </summary>
        bool TryForwardLookup(string hostname, out IList<IPAddress> addresses);
        /// <summary>
        /// Try to resolve the specified <see cref="IPAddress"/> to a hostname.
        /// </summary>
        bool TryReverseLookup(IPAddress address, out IList<string> hostnames);
    }
}
