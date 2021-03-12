using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a client capable of returning a DNS answer for a query.
    /// </summary>
    public interface IDnsClient : IDisposable
    {
        /// <summary>
        /// Return an answer for the specified DNS query.
        /// </summary>
        Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default);
    }
}
