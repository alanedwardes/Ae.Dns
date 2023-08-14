﻿using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
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

        private AddressFamily ToAddressFamily(DnsQueryType queryType)
        {
            return queryType switch
            {
                DnsQueryType.A => AddressFamily.InterNetwork,
                DnsQueryType.AAAA => AddressFamily.InterNetworkV6,
                _ => AddressFamily.Unknown,
            };
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            if (query.TryParseIpAddressFromReverseLookup(out var reverseLookupAddress))
            {
                foreach (var source in _sources)
                {
                    if (source.TryReverseLookup(reverseLookupAddress, out var foundHosts))
                    {
                        return ReturnPointer(query, foundHosts, source);
                    }
                }
            }

            foreach (var source in _sources)
            {
                if (source.TryForwardLookup(query.Header.Host, out var addresses))
                {
                    // This might return zero addresses, but that's OK. We must not return an error.
                    // For reasoning behind this, see https://www.rfc-editor.org/rfc/rfc4074#section-3
                    return ReturnAddresses(query, addresses.Where(x => x.AddressFamily == ToAddressFamily(query.Header.QueryType)), source);
                }
            }

            return await _dnsClient.Query(query, token);
        }

        private DnsMessage ReturnAddresses(DnsMessage query, IEnumerable<IPAddress> addresses, IDnsLookupSource lookupSource)
        {
            return new DnsMessage
            {
                Header = CreateAnswer(query, lookupSource, (short)addresses.Count()),
                Answers = addresses.Select(address => new DnsResourceRecord
                {
                    Class = DnsQueryClass.IN,
                    Host = query.Header.Host,
                    Resource = new DnsIpAddressResource { IPAddress = address },
                    Type = address.AddressFamily == AddressFamily.InterNetworkV6 ? DnsQueryType.AAAA : DnsQueryType.A,
                    TimeToLive = 3600
                }).ToList()
            };
        }

        private DnsMessage ReturnPointer(DnsMessage query, IEnumerable<string> foundHosts, IDnsLookupSource lookupSource)
        {
            return new DnsMessage
            {
                Header = CreateAnswer(query, lookupSource, (short)foundHosts.Count()),
                Answers = foundHosts.Select(foundHost => new DnsResourceRecord
                {
                    Class = DnsQueryClass.IN,
                    Host = query.Header.Host,
                    Resource = new DnsDomainResource { Entries = foundHost.Split('.') },
                    Type = DnsQueryType.PTR,
                    TimeToLive = 3600
                }).ToList()
            };
        }

        private DnsHeader CreateAnswer(DnsMessage query, IDnsLookupSource lookupSource, short answers) => new DnsHeader
        {
            Id = query.Header.Id,
            ResponseCode = DnsResponseCode.NoError,
            IsQueryResponse = true,
            RecursionAvailable = true,
            AnswerRecordCount = answers,
            AuthoritativeAnswer = true,
            RecursionDesired = query.Header.RecursionDesired,
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
