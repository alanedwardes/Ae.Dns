using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public sealed class DnsFilterClient : IDnsClient
    {
        private readonly ILogger<DnsFilterClient> _logger;
        private readonly IDnsFilter _dnsFilter;
        private readonly IDnsClient _dnsClient;

        public DnsFilterClient(IDnsFilter dnsFilter, IDnsClient dnsClient)
            : this(new NullLogger<DnsFilterClient>(), dnsFilter, dnsClient)
        {
        }

        public DnsFilterClient(ILogger<DnsFilterClient> logger, IDnsFilter dnsFilter, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsFilter = dnsFilter;
            _dnsClient = dnsClient;
        }

        public void Dispose()
        {
        }

        private DnsHeader CreateNullHeader(DnsHeader query) => new DnsHeader
        {
            Id = query.Id,
            ResponseCode = DnsResponseCode.NXDomain,
            IsQueryResponse = true,
            RecursionAvailable = true,
            RecusionDesired = query.RecusionDesired,
            Host = query.Host,
            QueryClass = query.QueryClass,
            QuestionCount = query.QuestionCount,
            QueryType = query.QueryType
        };

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            if (_dnsFilter.IsPermitted(query))
            {
                return await _dnsClient.Query(query, token);
            }

            _logger.LogTrace("DNS query blocked for {Domain}", query.Host);
            return new DnsAnswer { Header = CreateNullHeader(query) };
        }
    }
}
