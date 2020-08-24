using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public sealed class DnsRoundRobinClient : IDnsClient
    {
        private readonly IDnsClient[] _dnsClients;

        public DnsRoundRobinClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        private IDnsClient GetRandomClient() => _dnsClients.OrderBy(x => Guid.NewGuid()).First();

        public Task<byte[]> LookupRaw(DnsHeader query) => GetRandomClient().LookupRaw(query);
    }
}
