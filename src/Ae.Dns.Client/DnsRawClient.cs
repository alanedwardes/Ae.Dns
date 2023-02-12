using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <inheritdoc/>
    public sealed class DnsRawClient : IDnsRawClient
    {
        private readonly ILogger<DnsRawClient> _logger;
        private readonly IDnsClient _dnsClient;

        /// <inheritdoc/>
        [ActivatorUtilitiesConstructor]
        public DnsRawClient(ILogger<DnsRawClient> logger, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public DnsRawClient(IDnsClient dnsClient) : this(NullLogger<DnsRawClient>.Instance, dnsClient)
        {
        }

        /// <inheritdoc/>
        public void Dispose() => _dnsClient.Dispose();

        /// <inheritdoc/>
        public async Task<DnsRawClientResponse> Query(Memory<byte> buffer, int queryLength, EndPoint querySource, CancellationToken token = default)
        {
            var queryBuffer = buffer.Slice(0, queryLength);

            DnsMessage query;
            try
            {
                query = DnsByteExtensions.FromBytes<DnsMessage>(queryBuffer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse incoming query {Bytes}", DnsByteExtensions.ToDebugString(queryBuffer.ToArray()));
                throw;
            }

            query.Header.Tags.Add("Sender", querySource);

            DnsMessage answer;
            try
            {
                answer = await _dnsClient.Query(query, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to resolve {Query}", query);
                throw;
            }

            var answerLength = 0;
            try
            {
                answer.WriteBytes(buffer, ref answerLength);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to serialise {Answer} for query {Query}", answer, query);
                throw;
            }

            _logger.LogTrace("Returning {Answer} for query {Query}", answer, query);
            return new DnsRawClientResponse(answerLength, query, answer);
        }
    }
}
