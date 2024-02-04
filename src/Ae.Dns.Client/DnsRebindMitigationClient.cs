using Ae.Dns.Client.Internal;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Microsoft.Extensions.Logging;
using System.Linq;
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
                _logger.LogTrace("DNS rebind attack mitigated for {query}", query);
                return query.CreateErrorMessage(DnsResponseCode.Refused, ToString());
            }

            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => nameof(DnsRebindMitigationClient);
    }
}
