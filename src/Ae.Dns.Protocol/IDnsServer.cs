using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a server capable of receiving DNS requests.
    /// </summary>
    public interface IDnsServer : IDisposable
    {
        /// <summary>
        /// Listen for DNS queries.
        /// </summary>
        /// <param name="token">The <see cref="CancellationToken"/> to use to stop listening.</param>
        /// <returns>A task which will run forever unless cancelled.</returns>
        Task Listen(CancellationToken token = default);
    }
}
