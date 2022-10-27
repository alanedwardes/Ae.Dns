using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsClientParallelismTest
    {
        [Fact]
        public async Task TestUdpClient() => await TestClient(new DnsUdpClient(IPAddress.Parse("8.8.8.8")));

        [Fact]
        public async Task TestTcpClient() => await TestClient(new DnsTcpClient(IPAddress.Parse("8.8.8.8")));

        [Fact]
        public async Task TestHttpClient() => await TestClient(new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://8.8.8.8/") }));

        private static async Task TestClient(IDnsClient client)
        {
            await Task.WhenAll(
                client.RunQuery("google.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("microsoft.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("apple.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("facebook.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("reddit.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("adobe.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("alanedwardes.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("amazon.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("nytimes.com", DnsQueryType.A, DnsResponseCode.NoError),
                client.RunQuery("washingtonpost.com", DnsQueryType.A, DnsResponseCode.NoError)
            );

            client.Dispose();
        }
    }
}
