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
        /// <param name="query">The DNS query to run, see <see cref="DnsQueryFactory"/>.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the operation.</param>
        /// <returns>The <see cref="DnsMessage"/> result.</returns>
        Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default);
    }
}
