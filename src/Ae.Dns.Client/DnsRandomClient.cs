using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client consisting of multiple <see cref="IDnsClient"/> instances which are picked at random.
    /// </summary>
    public sealed class DnsRandomClient : IDnsClient
    {
        private readonly IReadOnlyCollection<IDnsClient> _dnsClients;

        /// <summary>
        /// Create a new random DNS client selector using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="dnsClients">The enumerable of DNS clients to use.</param>
        [ActivatorUtilitiesConstructor]
        public DnsRandomClient(IEnumerable<IDnsClient> dnsClients) => _dnsClients = dnsClients.ToList();

        /// <summary>
        /// Create a new random DNS client selector using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="dnsClients">The array of DNS clients to use.</param>
        public DnsRandomClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        private IDnsClient GetRandomClient() => _dnsClients.OrderBy(x => Guid.NewGuid()).First();

        /// <inheritdoc/>
        public Task<DnsMessage> Query(DnsMessage query, CancellationToken token) => GetRandomClient().Query(query, token);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
