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
    public sealed class DnsRecursiveClient : IDnsClient
    {
        private readonly IReadOnlyList<IDnsClient> _rootServerClients = DnsRootServer.All.Select(x => new DnsUdpClient(x.Ipv4Address)).ToArray();

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

            while (true)
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

                var answer = await dnsClient.Query(query, token);
                if (answer.Answers.Any())
                {
                    return answer;
                }

                if (!answer.Additional.Any())
                {
                    var nameserver = Random(answer.Nameservers.Where(x => x.Type == DnsQueryType.NS).Select(x => x.Resource).Cast<DnsTextResource>());

                    var answer1 = await Query(DnsQueryFactory.CreateQuery(nameserver.Text, DnsQueryType.A), token);

                    lookup = Random(answer.Answers.Where(x => x.Type == DnsQueryType.A).Select(x => x.Resource).Cast<DnsIpAddressResource>());
                    continue;
                }

                lookup = Random(answer.Additional.Where(x => x.Type == DnsQueryType.A).Select(x => x.Resource).Cast<DnsIpAddressResource>());
            }
        }
    }
}

