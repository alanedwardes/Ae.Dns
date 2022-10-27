using Ae.Dns.Protocol;
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
        private const string DnsMessageType = "application/dns-message";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new DNS HTTP client using the specified <see cref="HttpClient"/> instance.
        /// </summary>
        public DnsHttpClient(HttpClient httpClient) => _httpClient = httpClient;

        /// <inheritdoc/>
        public void Dispose() => _httpClient?.Dispose();

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var queryBuffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

            var queryBufferLength = 0;
            query.WriteBytes(queryBuffer, ref queryBufferLength);

            using var content = new ReadOnlyMemoryContent(queryBuffer.Slice(0, queryBufferLength));
            content.Headers.ContentType = new MediaTypeHeaderValue(DnsMessageType);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/dns-query") { Content = content };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DnsMessageType));

            using var response = await _httpClient.SendAsync(request, token);
            response.EnsureSuccessStatusCode();

#if NETSTANDARD2_1
            var buffer = await response.Content.ReadAsByteArrayAsync();
#else
            var buffer = await response.Content.ReadAsByteArrayAsync(token);
#endif

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(buffer);
            answer.Header.Tags.Add("Resolver", ToString());
            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => _httpClient.BaseAddress.ToString();
    }
}
