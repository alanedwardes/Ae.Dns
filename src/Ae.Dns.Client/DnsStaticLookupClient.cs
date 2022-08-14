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

namespace Ae.Dns.Client
{
    /// <summary>
    /// A DNS client providing a static lookup (similar to a hosts file).
    /// If the host is not found in that lookup, the request is forwarded to the other specified <see cref="IDnsClient"/>.
    /// </summary>
    public sealed class DnsStaticLookupClient : IDnsClient
    {
        private readonly IReadOnlyDictionary<string, IPAddress> _lookup;
        private readonly IReadOnlyDictionary<string, string> _reverseLookup;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsStaticLookupClient"/> using the specified lookup map.
        /// </summary>
        public DnsStaticLookupClient(IReadOnlyDictionary<string, IPAddress> lookup, IDnsClient dnsClient)
        {
            _lookup = lookup;
            _reverseLookup = BuildReverseLookup(lookup);
            _dnsClient = dnsClient;
        }

        private IReadOnlyDictionary<string, string> BuildReverseLookup(IReadOnlyDictionary<string, IPAddress> lookup)
        {
            static string CreateReverseQuery(IPAddress address)
            {
                // 192.168.1.1 becomes 1.1.168.192.in-addr.arpa
                return string.Join(".", address.ToString().Split('.').Reverse()) + ".in-addr.arpa";
            }

            // Only the first found hostname is used if an address is bound to multiple host names
            return lookup.GroupBy(x => x.Value).ToDictionary(x => CreateReverseQuery(x.Key), x => x.First().Key);
        }

        public void Dispose()
        {
        }

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            if (query.QueryType == DnsQueryType.PTR && _reverseLookup.TryGetValue(query.Host, out var foundHost))
            {
                return ReturnPointer(query, foundHost);
            }

            if ((query.QueryType == DnsQueryType.A || query.QueryType == DnsQueryType.AAAA) && _lookup.TryGetValue(query.Host, out IPAddress address))
            {
                return ReturnAddress(query, address);
            }

            return await _dnsClient.Query(query, token);
        }

        private DnsAnswer ReturnAddress(DnsHeader query, IPAddress address)
        {
            return new DnsAnswer
            {
                Header = CreateAnswer(query),
                Answers = new List<DnsResourceRecord>
                    {
                        new DnsResourceRecord
                        {
                            Class = DnsQueryClass.IN,
                            Host = query.Host,
                            Resource = new DnsIpAddressResource{IPAddress = address},
                            Type = address.AddressFamily == AddressFamily.InterNetworkV6 ? DnsQueryType.AAAA : DnsQueryType.A,
                            TimeToLive = TimeSpan.FromHours(1)
                        }
                    }
            };
        }

        private DnsAnswer ReturnPointer(DnsHeader query, string foundHost)
        {
            return new DnsAnswer
            {
                Header = CreateAnswer(query),
                Answers = new List<DnsResourceRecord>
                    {
                        new DnsResourceRecord
                        {
                            Class = DnsQueryClass.IN,
                            Host = query.Host,
                            Resource = new DnsTextResource{Text = foundHost},
                            Type = DnsQueryType.PTR,
                            TimeToLive = TimeSpan.FromHours(1)
                        }
                    }
            };
        }

        private DnsHeader CreateAnswer(DnsHeader query) => new DnsHeader
        {
            Id = query.Id,
            ResponseCode = DnsResponseCode.NoError,
            IsQueryResponse = true,
            RecursionAvailable = true,
            AnswerRecordCount = 1,
            RecusionDesired = query.RecusionDesired,
            Host = query.Host,
            QueryClass = query.QueryClass,
            QuestionCount = query.QuestionCount,
            QueryType = query.QueryType
        };
    }
}
