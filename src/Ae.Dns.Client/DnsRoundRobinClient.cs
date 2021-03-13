using Ae.Dns.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public sealed class DnsRoundRobinClient : IDnsClient
    {
        private readonly IReadOnlyCollection<IDnsClient> _dnsClients;

        public DnsRoundRobinClient(IEnumerable<IDnsClient> dnsClients) => _dnsClients = dnsClients.ToList();

        public DnsRoundRobinClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        private IDnsClient GetRandomClient() => _dnsClients.OrderBy(x => Guid.NewGuid()).First();

        /// <inheritdoc/>
        public Task<DnsAnswer> Query(DnsHeader query, CancellationToken token) => GetRandomClient().Query(query, token);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
