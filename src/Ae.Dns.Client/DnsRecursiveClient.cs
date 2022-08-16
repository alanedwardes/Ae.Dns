using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a recursive DNS client which starts at the root servers to answer queries.
    /// </summary>
    [Obsolete("This class is experimental.")]
    public sealed class DnsRecursiveClient : IDnsClient
    {
        private readonly IReadOnlyList<IDnsClient> _rootServerClients = DnsRootServer.All.Select(x => new DnsUdpClient(x.Ipv4Address)).ToArray();
        private readonly DnsQueryType _ipAddressQueryType;

        /// <summary>
        /// Create a new <see cref="DnsRecursiveClient"/> optionally using IPv6.
        /// </summary>
        /// <param name="internetProtocolV4"></param>
        public DnsRecursiveClient(bool internetProtocolV4 = true)
        {
            _ipAddressQueryType = internetProtocolV4 ? DnsQueryType.A : DnsQueryType.AAAA;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var client in _rootServerClients)
            {
                client.Dispose();
            }
        }

        private TItem Random<TItem>(IEnumerable<TItem> items) => items.OrderBy(x => Guid.NewGuid()).First();

        private TRecord RandomRecord<TRecord>(IEnumerable<DnsResourceRecord> records, DnsQueryType type)
        {
            return Random(records.Where(x => x.Type == type).Select(x => x.Resource).Cast<TRecord>());
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default) => await QueryRecursive(query, 0, token);

        private async Task<DnsMessage> QueryRecursive(DnsMessage query, int depth, CancellationToken token = default)
        {
            int lookups = 0;
            DnsIpAddressResource lookup = null;

            while (depth < 3 && lookups < 5)
            {
                using IDnsClient dnsClient = lookup == null ? null : new DnsUdpClient(lookup.IPAddress);

                lookups++;
                query.Header.Id = DnsQueryFactory.GenerateId();
                var nameserverAnswer = await (dnsClient ?? Random(_rootServerClients)).Query(query, token);
                if (nameserverAnswer.Answers.Any())
                {
                    return nameserverAnswer;
                }

                if (nameserverAnswer.Additional.Any())
                {
                    lookup = RandomRecord<DnsIpAddressResource>(nameserverAnswer.Additional, _ipAddressQueryType);
                    continue;
                }

                if (nameserverAnswer.Nameservers.Any())
                {
                    string nameserver = RandomRecord<DnsTextResource>(nameserverAnswer.Nameservers, DnsQueryType.NS).Text;
                    lookup = await LookupNameserverIpAddress(nameserver, depth, token);
                    continue;
                }

                lookup = await LookupNameserverIpAddress(query.Header.Host, depth, token);
            }

            throw new DnsClientException($"Too much recursion ({depth}) or too many lookups ({lookups})", query.Header.Host);
        }

        private async Task<DnsIpAddressResource> LookupNameserverIpAddress(string nameserver, int depth, CancellationToken token)
        {
            var nameserverAddressLookup = DnsQueryFactory.CreateQuery(nameserver, _ipAddressQueryType);
            var nameserverAddressAnswer = await QueryRecursive(nameserverAddressLookup, depth++, token);

            if (nameserverAddressAnswer.Answers.Any(x => x.Type == _ipAddressQueryType))
            {
                return RandomRecord<DnsIpAddressResource>(nameserverAddressAnswer.Answers, _ipAddressQueryType);
            }

            if (nameserverAddressAnswer.Answers.Any(x => x.Type == DnsQueryType.CNAME))
            {
                var cname = RandomRecord<DnsTextResource>(nameserverAddressAnswer.Answers, DnsQueryType.CNAME);
                return await LookupNameserverIpAddress(cname.Text, depth, token);
            }

            throw new DnsClientException($"Unable to process answer {nameserverAddressAnswer}", nameserver);
        }
    }
}

