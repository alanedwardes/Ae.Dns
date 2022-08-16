using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsDelegatingHandlerTests : MockTestClass
    {
        [Theory]
        [InlineData(true, DnsQueryType.A, "1.2.3.4")]
        [InlineData(false, DnsQueryType.AAAA, "[2001:db8:85a3::8a2e:370:7334]")]
        public async Task TestDelegatingHandler(bool isIpv4, DnsQueryType dnsQueryType, string address)
        {
            var dnsClient = Repository.Create<IDnsClient>();
            var mockHandler = new Mock<MockHttpMessageHandler> { CallBase = true };
            var dnsHandler = new DnsDelegatingHandler(dnsClient.Object, isIpv4) { InnerHandler = mockHandler.Object };
            var httpClient = new HttpClient(dnsHandler);

            dnsClient.Setup(x => x.Query(It.Is<DnsMessage>(x => x.Header.Host == "www.google.com" && x.Header.QueryType == dnsQueryType), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new DnsMessage
                     {
                         Answers = new List<DnsResourceRecord>
                         {
                             new DnsResourceRecord
                             {
                                 Type = dnsQueryType,
                                 Resource = new DnsIpAddressResource{ IPAddress = IPAddress.Parse(address) }
                             }
                         }
                     });

            var expectedResponse = new HttpResponseMessage();

            mockHandler.Setup(x => x.SendAsyncMock(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((request, CancellationToken) =>
                {
                    Assert.Equal(HttpMethod.Get, request.Method);
                    Assert.Equal("www.google.com", request.Headers.Host);
                    Assert.Equal($"https://{address}/test", request.RequestUri.ToString());
                })
                .ReturnsAsync(expectedResponse);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/test");

            Assert.Equal("https://www.google.com/test", request.RequestUri.ToString());
            Assert.Null(request.Headers.Host);

            var actualResponse = await httpClient.SendAsync(request);
            Assert.Same(expectedResponse, actualResponse);

            Assert.Equal("https://www.google.com/test", request.RequestUri.ToString());
            Assert.Null(request.Headers.Host);
        }
    }
}
