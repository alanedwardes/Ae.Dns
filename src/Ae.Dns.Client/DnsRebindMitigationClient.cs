using Ae.Dns.Client.Internal;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client which prevents an upstream from returning a DNS record which contains a private IP address.
    /// </summary>
    public sealed class DnsRebindMitigationClient : IDnsClient
    {
        private readonly ILogger<DnsRebindMitigationClient> _logger;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsRebindMitigationClient"/> using the specified <see cref="ILogger"/> and <see cref="IDnsClient"/>.
        /// </summary>
        public DnsRebindMitigationClient(ILogger<DnsRebindMitigationClient> logger, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            var answer = await _dnsClient.Query(query, token);

            var ipAddressResponses = answer.Answers.Where(x => x.Type == DnsQueryType.A || x.Type == DnsQueryType.AAAA)
                .Select(x => x.Resource)
                .Cast<DnsIpAddressResource>()
                .Select(x => x.IPAddress);

            if (ipAddressResponses.Any(IpAddressExtensions.IsPrivate))
            {
                _logger.LogWarning("DNS rebind attack mitigated for {query}", query);
                return CreateNullHeader(query);
            }

            return answer;
        }

        private DnsMessage CreateNullHeader(DnsMessage query) => new DnsMessage
        {
            Header = new DnsHeader
            {
                Id = query.Header.Id,
                ResponseCode = DnsResponseCode.Refused,
                IsQueryResponse = true,
                RecursionAvailable = true,
                RecusionDesired = query.Header.RecusionDesired,
                Host = query.Header.Host,
                QueryClass = query.Header.QueryClass,
                QuestionCount = query.Header.QuestionCount,
                QueryType = query.Header.QueryType,
                Tags = { { "Resolver", ToString() } }
            }
        };

        /// <inheritdoc/>
        public override string ToString() => nameof(DnsRebindMitigationClient);
    }
}
