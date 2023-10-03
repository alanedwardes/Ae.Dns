using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly DnsHttpClientOptions _options;
        private const string DnsMessageType = "application/dns-message";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new DNS HTTP client using the specified <see cref="DnsHttpClientOptions"/> and <see cref="HttpClient"/> instance.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public DnsHttpClient(IOptions<DnsHttpClientOptions> options, HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Create a new DNS HTTP client using the specified <see cref="HttpClient"/> instance.
        /// </summary>
        public DnsHttpClient(HttpClient httpClient) : this(Options.Create(new DnsHttpClientOptions()), httpClient)
        {
        }

        /// <inheritdoc/>
        public void Dispose() => _httpClient?.Dispose();

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var queryBuffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

            var queryBufferLength = 0;
            query.WriteBytes(queryBuffer, ref queryBufferLength);

#if NETSTANDARD2_0
            using var content = new ByteArrayContent(queryBuffer.Array, 0, queryBufferLength);
#else
            using var content = new ReadOnlyMemoryContent(queryBuffer.Slice(0, queryBufferLength));
#endif
            content.Headers.ContentType = new MediaTypeHeaderValue(DnsMessageType);

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Path) { Content = content };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DnsMessageType));

            using var response = await _httpClient.SendAsync(request, token);
            response.EnsureSuccessStatusCode();

#if NETSTANDARD2_0 || NETSTANDARD2_1
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
