using Ae.DnsResolver.Client;
using Ae.DnsResolver.Client.Exceptions;
using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.DnsResolver.Tests.Client
{
    public class DnsUdpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesCom()
        {
            DnsAnswer answer;
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("1.1.1.1"), "test"))
            {
                answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"));
            }

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupCpscCom()
        {
            DnsAnswer answer;
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("8.8.8.8"), "test"))
            {
                answer = await client.Query(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));
            }

            Assert.Empty(answer.Answers);
            Assert.True(answer.Header.Truncation);
        }

        [Fact]
        public async Task TestLookupTimeout()
        {
            // Reserved - see https://en.wikipedia.org/wiki/Reserved_IP_addresses
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("192.88.99.0"), "test"))
            {
                await Assert.ThrowsAsync<DnsClientTimeoutException>(() => client.Query(DnsHeader.CreateQuery("alanedwardes.com")));
            }
        }
    }
}
