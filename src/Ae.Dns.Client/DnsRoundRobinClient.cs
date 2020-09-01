using Ae.Dns.Protocol;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public sealed class DnsRoundRobinClient : IDnsClient
    {
        private readonly IDnsClient[] _dnsClients;

        public DnsRoundRobinClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        private IDnsClient GetRandomClient() => _dnsClients.OrderBy(x => Guid.NewGuid()).First();

        public Task<DnsAnswer> Query(DnsHeader query, CancellationToken token) => GetRandomClient().Query(query, token);
    }
}
