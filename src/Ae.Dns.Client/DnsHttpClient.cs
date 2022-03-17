using Ae.Dns.Protocol;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a DNS client which operates over HTTPS (DoH).
    /// </summary>
    public sealed class DnsHttpClient : IDnsClient
    {
        private readonly Meter _meter;
        private readonly Counter<int> _successCounter;
        private readonly Counter<int> _failureCounter;

        private const string DnsMessageType = "application/dns-message";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new DNS HTTP client using the specified <see cref="HttpClient"/> instance.
        /// </summary>
        public DnsHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _meter = new Meter($"Ae.Dns.Client.DnsHttpClient.{httpClient.BaseAddress.Host}");
            _successCounter = _meter.CreateCounter<int>("Success");
            _failureCounter = _meter.CreateCounter<int>("Failure");
    }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var queryMetricState = new KeyValuePair<string, object>("Query", query);
            var upstreamMetricState = new KeyValuePair<string, object>("Address", _httpClient.BaseAddress);

            var raw = DnsByteExtensions.ToBytes(query).ToArray();

            var content = new ByteArrayContent(raw);
            content.Headers.ContentType = new MediaTypeHeaderValue(DnsMessageType);

            var request = new HttpRequestMessage(HttpMethod.Post, "/dns-query")
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DnsMessageType));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, token);
                response.EnsureSuccessStatusCode();
                _successCounter.Add(1, upstreamMetricState, queryMetricState);
            }
            catch
            {
                _failureCounter.Add(1, upstreamMetricState, queryMetricState);
                throw;
            }

            return DnsByteExtensions.FromBytes<DnsAnswer>(await response.Content.ReadAsByteArrayAsync());
        }
    }
}
