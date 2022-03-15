using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a DNS client which supports filtering.
    /// </summary>
    public sealed class DnsFilterClient : IDnsClient
    {
        private static readonly Meter _meter = new Meter("Ae.Dns.Client.DnsFilterClient");
        private static readonly Counter<int> _blockedCounter = _meter.CreateCounter<int>("blocked");
        private static readonly Counter<int> _allowedCounter = _meter.CreateCounter<int>("allowed");

        private readonly ILogger<DnsFilterClient> _logger;
        private readonly IDnsFilter _dnsFilter;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Create a new filter client using the specified <see cref="IDnsFilter"/> and <see cref="IDnsClient"/>.
        /// </summary>
        public DnsFilterClient(IDnsFilter dnsFilter, IDnsClient dnsClient)
            : this(new NullLogger<DnsFilterClient>(), dnsFilter, dnsClient)
        {
        }

        /// <summary>
        /// Create a new filter client using the specified logger, <see cref="IDnsFilter"/> and <see cref="IDnsClient"/>.
        /// </summary>
        public DnsFilterClient(ILogger<DnsFilterClient> logger, IDnsFilter dnsFilter, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsFilter = dnsFilter;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            var queryMetricState = new KeyValuePair<string, object>("Query", query);

            if (_dnsFilter.IsPermitted(query))
            {
                _allowedCounter.Add(1, queryMetricState);
                return await _dnsClient.Query(query, token);
            }

            _blockedCounter.Add(1, queryMetricState);
            _logger.LogInformation("DNS query blocked for {Domain}", query.Host);
            return new DnsAnswer { Header = CreateNullHeader(query) };
        }
    }
}
