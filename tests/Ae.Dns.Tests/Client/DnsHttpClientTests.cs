using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsHttpClientTests
    {
        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithCloudFlare(string domain, DnsQueryType type)
        {
            using var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://cloudflare-dns.com/") });
            await client.RunQuery(domain, type, type == DnsQueryType.ANY ? DnsResponseCode.NotImp : DnsResponseCode.NoError);
        }

        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithGoogle(string domain, DnsQueryType type)
        {
            using var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });
            await client.RunQuery(domain, type);
        }

        [Fact]
        public async Task TestLookupMock()
        {
            var mockHandler = new Mock<MockHttpMessageHandler> { CallBase = true };
            using var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://www.example.com") };
            using var dnsClient = new DnsHttpClient(Options.Create(new DnsHttpClientOptions { Path = "/wibble" }), httpClient);

            mockHandler.Setup(x => x.SendAsyncMock(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                       .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                       {
                           Assert.Equal(HttpMethod.Post, request.Method);
                           Assert.Equal("https://www.example.com/wibble", request.RequestUri.ToString());
                           Assert.Equal("application/dns-message", request.Headers.Accept.Single().ToString());

                           var expectedQueryBytes = SampleDnsPackets.Query2;
                           var actualQueryBytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                           // Don't compare the "packet ID" but compare the rest of the query
                           Assert.Equal(expectedQueryBytes.Skip(2), actualQueryBytes.Skip(2));
                       })
                       .ReturnsAsync(new HttpResponseMessage { Content = new ByteArrayContent(SampleDnsPackets.Answer1) });

            await dnsClient.Query(DnsQueryFactory.CreateQuery("polling.bbc.co.uk"), CancellationToken.None);
        }
    }
}
