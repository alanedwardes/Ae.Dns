using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// A DNS client providing a static lookup (similar to a hosts file).
    /// If the host is not found in that lookup, the request is forwarded to the other specified <see cref="IDnsClient"/>.
    /// </summary>
    public sealed class DnsStaticLookupClient : IDnsClient
    {
        private readonly IEnumerable<IDnsLookupSource> _sources;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsStaticLookupClient"/> using the specified lookup map.
        /// </summary>
        public DnsStaticLookupClient(IDnsClient dnsClient, params IDnsLookupSource[] sources)
        {
            _sources = sources;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var source in _sources)
            {
                source.Dispose();
            }
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            if (query.Header.QueryType == DnsQueryType.PTR)
            {
                foreach (var source in _sources)
                {
                    var addressStringFromHost = string.Join(".", query.Header.Host.Split('.').Take(4).Reverse());
                    if (IPAddress.TryParse(addressStringFromHost, out var address) && source.TryReverseLookup(address, out var foundHost))
                    {
                        return ReturnPointer(query, foundHost, source);
                    }
                }
            }

            if (query.Header.QueryType == DnsQueryType.A || query.Header.QueryType == DnsQueryType.AAAA)
            {
                foreach (var source in _sources)
                {
                    if (source.TryForwardLookup(query.Header.Host, out IPAddress address))
                    {
                        return ReturnAddress(query, address, source);
                    }
                }
            }

            return await _dnsClient.Query(query, token);
        }

        private DnsMessage ReturnAddress(DnsMessage query, IPAddress address, IDnsLookupSource lookupSource)
        {
            return new DnsMessage
            {
                Header = CreateAnswer(query, lookupSource),
                Answers = new List<DnsResourceRecord>
                {
                    new DnsResourceRecord
                    {
                        Class = DnsQueryClass.IN,
                        Host = query.Header.Host,
                        Resource = new DnsIpAddressResource{IPAddress = address},
                        Type = address.AddressFamily == AddressFamily.InterNetworkV6 ? DnsQueryType.AAAA : DnsQueryType.A,
                        TimeToLive = 3600
                    }
                }
            };
        }

        private DnsMessage ReturnPointer(DnsMessage query, string foundHost, IDnsLookupSource lookupSource)
        {
            return new DnsMessage
            {
                Header = CreateAnswer(query, lookupSource),
                Answers = new List<DnsResourceRecord>
                {
                    new DnsResourceRecord
                    {
                        Class = DnsQueryClass.IN,
                        Host = query.Header.Host,
                        Resource = new DnsTextResource{Text = foundHost},
                        Type = DnsQueryType.PTR,
                        TimeToLive = 3600
                    }
                }
            };
        }

        private DnsHeader CreateAnswer(DnsMessage query, IDnsLookupSource lookupSource) => new DnsHeader
        {
            Id = query.Header.Id,
            ResponseCode = DnsResponseCode.NoError,
            IsQueryResponse = true,
            RecursionAvailable = true,
            AnswerRecordCount = 1,
            RecusionDesired = query.Header.RecusionDesired,
            Host = query.Header.Host,
            QueryClass = query.Header.QueryClass,
            QuestionCount = query.Header.QuestionCount,
            QueryType = query.Header.QueryType,
            Tags = { { "Resolver", $"{nameof(DnsStaticLookupClient)}({lookupSource})" } }
        };

        /// <inheritdoc/>
        public override string ToString() => nameof(DnsStaticLookupClient);
    }
}
