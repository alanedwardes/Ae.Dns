using Ae.Dns.Protocol;
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
        private const string DnsMessageType = "application/dns-message";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new DNS HTTP client using the specified <see cref="HttpClient"/> instance.
        /// </summary>
        public DnsHttpClient(HttpClient httpClient) => _httpClient = httpClient;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var raw = query.ToBytes().ToArray();

            var content = new ByteArrayContent(raw);
            content.Headers.ContentType = new MediaTypeHeaderValue(DnsMessageType);

            var request = new HttpRequestMessage(HttpMethod.Post, "/dns-query")
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DnsMessageType));

            var response = await _httpClient.SendAsync(request, token);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadAsByteArrayAsync()).FromBytes<DnsAnswer>();
        }
    }
}
