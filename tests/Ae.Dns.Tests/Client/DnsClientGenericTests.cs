using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsClientGenericTests
    {
        [Fact]
        public async Task TestUdpClientParallel() => await TestClientParallel(new DnsUdpClient(IPAddress.Parse("8.8.8.8")));

        [Fact]
        public async Task TestTcpClientParallel() => await TestClientParallel(new DnsTcpClient(IPAddress.Parse("8.8.8.8")));

        [Fact]
        public async Task TestHttpClientParallel() => await TestClientParallel(new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://8.8.8.8/") }));

        private static async Task TestClientParallel(IDnsClient client)
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

        [Fact]
        public async Task TestHttpClientLargeResponse1() => await TestClientLargeResponse1(new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://8.8.8.8/") }));

        [Fact]
        public async Task TestTcpClientLargeResponse1() => await TestClientLargeResponse1(new DnsTcpClient(IPAddress.Parse("8.8.8.8")));

        [Fact]
        public async Task TestUdpClientLargeResponse1() => await TestClientLargeResponse1(new DnsUdpClient(IPAddress.Parse("8.8.8.8")), true);

        private static async Task TestClientLargeResponse1(IDnsClient client, bool truncated = false)
        {
            var result = await client.Query(DnsQueryFactory.CreateQuery("dnstest1.alanedwardes.com", DnsQueryType.TEXT));

            if (truncated)
            {
                Assert.True(result.Header.Truncation);
                Assert.Empty(result.Answers);
            }
            else
            {
                var answers = result.Answers.Select(x => x.Resource).Cast<DnsStringResource>().Select(x => x.Entries.Single()).ToArray();

                Assert.Equal(29, answers.Length);

                Assert.Equal("Aliquam erat volutpat. Sed tempus sagittis nulla quis ullamcorper. Vestibulum at cursus arcu. Ut rutrum molestie est, et faucibus nisi iaculis et. Vestibulum ac venenatis est, a tincidunt turpis.", answers[0]);
                Assert.Equal("Cras sed nulla nibh. Morbi convallis venenatis purus ut condimentum. Fusce enim velit, cursus ut elementum id, dignissim vitae lacus.", answers[1]);
                Assert.Equal("Donec vel sapien sed tortor mollis interdum. Suspendisse tortor nisl, molestie eget nunc eget, tempor tincidunt diam.", answers[2]);
                Assert.Equal("Fusce vel augue ac eros ullamcorper pharetra mattis non mauris. Sed id nibh consequat, lobortis risus sit amet, molestie libero. Sed augue sapien, imperdiet sed mollis sit amet, pellentesque quis purus.", answers[3]);
                Assert.Equal("In ultrices mi nec mauris volutpat tincidunt. Fusce nisl ligula, venenatis non elit sit amet, pretium semper quam.", answers[4]);
                Assert.Equal("Integer accumsan diam augue, congue mattis nunc cursus sed. Integer in sodales enim, eu venenatis leo. Vestibulum augue arcu, porta at sodales vel, sollicitudin molestie quam.", answers[5]);
                Assert.Equal("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent quis sagittis est. Donec imperdiet nunc lacinia quam eleifend tincidunt. Duis vel arcu orci.", answers[6]);
                Assert.Equal("Maecenas quis mi et ante volutpat consectetur. Donec eleifend odio in efficitur elementum. Aliquam sit amet ipsum non purus convallis cursus. Fusce quis risus nunc. Nullam et suscipit odio, quis maximus sapien.", answers[7]);
                Assert.Equal("Morbi id felis eget risus tempus porttitor at a metus. Cras a eros porta, interdum libero eu, hendrerit urna. Integer semper aliquam nisi, at bibendum diam dignissim et. Suspendisse odio dui, pharetra gravida fermentum vel, varius et tellus.", answers[8]);
                Assert.Equal("Nam eu nisl mauris. Maecenas at risus sit amet enim vestibulum efficitur sit amet quis quam.", answers[9]);
            }

            client.Dispose();
        }
    }
}
