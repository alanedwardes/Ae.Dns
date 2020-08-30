using Ae.DnsResolver.Protocol;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public sealed class DnsHttpClient : IDnsClient
    {
        private const string DnsMessageType = "application/dns-message";
        private readonly HttpClient _httpClient;

        public DnsHttpClient(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<DnsAnswer> Query(DnsHeader query)
        {
            var raw = query.WriteDnsHeader().ToArray();

            var content = new ByteArrayContent(raw);
            content.Headers.ContentType = new MediaTypeHeaderValue(DnsMessageType);

            var request = new HttpRequestMessage(HttpMethod.Post, "/dns-query")
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DnsMessageType));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var offset = 0;
            return (await response.Content.ReadAsByteArrayAsync()).ReadDnsAnswer(ref offset);
        }
    }
}
