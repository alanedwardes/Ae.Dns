using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;

namespace Ae.Dns.Client
{
    internal sealed class DnsRecursiveClient : IDnsClient
    {
        private readonly IReadOnlyList<IDnsClient> _rootServerClients = DnsRootServers.All.Select(x => new DnsUdpClient(x.Ipv4Address)).ToArray();

        public void Dispose()
        {
            foreach (var client in _rootServerClients)
            {
                client.Dispose();
            }
        }

        private TItem Random<TItem>(IEnumerable<TItem> items) => items.OrderBy(x => Guid.NewGuid()).First();

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            DnsIpAddressResource lookup = null;

            foreach (var part in query.Host.Split('.').Reverse())
            {
                IDnsClient dnsClient;

                if (lookup == null)
                {
                    dnsClient = Random(_rootServerClients);
                }
                else
                {
                    dnsClient = new DnsUdpClient(lookup.IPAddress);
                }

                var answer = await dnsClient.Query(DnsQueryFactory.CreateQuery(part, DnsQueryType.NS), token);

                lookup = Random(answer.Additional.Where(x => x.Type == DnsQueryType.A).Select(x => x.Resource).Cast<DnsIpAddressResource>());


            }

            throw new NotImplementedException();
        }
    }
}

