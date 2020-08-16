using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public sealed class DnsCompositeClient : IDnsClient
    {
        private readonly IDnsClient[] _dnsClients;

        public DnsCompositeClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        public Task<byte[]> LookupRaw(byte[] raw) => _dnsClients.OrderBy(x => Guid.NewGuid()).First().LookupRaw(raw);
    }
}
