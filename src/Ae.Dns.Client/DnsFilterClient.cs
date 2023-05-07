﻿using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a DNS client which supports filtering.
    /// </summary>
    public sealed class DnsFilterClient : IDnsClient
    {
        private readonly ILogger<DnsFilterClient> _logger;
        private readonly IDnsFilter _dnsFilter;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Create a new filter client using the specified logger, <see cref="IDnsFilter"/> and <see cref="IDnsClient"/>.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public DnsFilterClient(ILogger<DnsFilterClient> logger, IDnsFilter dnsFilter, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsFilter = dnsFilter;
            _dnsClient = dnsClient;
        }

        /// <summary>
        /// Create a new filter client using the specified <see cref="IDnsFilter"/> and <see cref="IDnsClient"/>.
        /// </summary>
        public DnsFilterClient(IDnsFilter dnsFilter, IDnsClient dnsClient)
            : this(NullLogger<DnsFilterClient>.Instance, dnsFilter, dnsClient)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private DnsHeader CreateNullHeader(DnsMessage query) => new DnsHeader
        {
            Id = query.Header.Id,
            ResponseCode = DnsResponseCode.Refused,
            IsQueryResponse = true,
            RecursionAvailable = true,
            RecursionDesired = query.Header.RecursionDesired,
            Host = query.Header.Host,
            QueryClass = query.Header.QueryClass,
            QuestionCount = query.Header.QuestionCount,
            QueryType = query.Header.QueryType,
            Tags = { { "Resolver", ToString() } }
        };

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            if (_dnsFilter.IsPermitted(query))
            {
                return await _dnsClient.Query(query, token);
            }

            _logger.LogInformation("DNS query blocked for {Domain}", query.Header.Host);
            return new DnsMessage { Header = CreateNullHeader(query) };
        }

        /// <inheritdoc/>
        public override string ToString() => nameof(DnsFilterClient);
    }
}
