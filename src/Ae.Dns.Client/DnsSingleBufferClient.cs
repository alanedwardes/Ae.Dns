using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <inheritdoc/>
    public sealed class DnsSingleBufferClient : IDnsSingleBufferClient
    {
        private readonly ILogger<DnsSingleBufferClient> _logger;
        private readonly IDnsClient _dnsClient;

        /// <inheritdoc/>
        public DnsSingleBufferClient(ILogger<DnsSingleBufferClient> logger, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public DnsSingleBufferClient(IDnsClient dnsClient) : this(NullLogger<DnsSingleBufferClient>.Instance, dnsClient)
        {
        }

        /// <inheritdoc/>
        public void Dispose() => _dnsClient.Dispose();

        /// <inheritdoc/>
        public async Task<DnsSingleBufferClientResponse> Query(Memory<byte> buffer, int queryLength, CancellationToken token = default)
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
            return new DnsSingleBufferClientResponse(answerLength, query, answer);
        }
    }
}
