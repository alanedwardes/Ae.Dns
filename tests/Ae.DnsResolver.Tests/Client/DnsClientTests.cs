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
    public class DnsClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesCom()
        {
            byte[] result;
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("1.1.1.1"), "test"))
            {
                result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com"));
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Length);
        }

        [Fact]
        public async Task TestLookupCpscCom()
        {
            byte[] result;
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("8.8.8.8"), "test"))
            {
                result = await client.LookupRaw(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);
            Assert.Empty(answer.Answers);
            Assert.True(answer.Header.Truncation);
        }

        [Fact]
        public async Task TestLookupTimeout()
        {
            // Reserved - see https://en.wikipedia.org/wiki/Reserved_IP_addresses
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("192.88.99.0"), "test"))
            {
                await Assert.ThrowsAsync<DnsClientTimeoutException>(() => client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com")));
            }
        }
    }
}
