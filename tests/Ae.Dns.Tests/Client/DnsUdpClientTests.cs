using Ae.Dns.Client;
using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsUdpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesCom()
        {
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("1.1.1.1"));

            await client.Query(DnsHeader.CreateQuery("alanedwardes.com"), CancellationToken.None);
        }

        [Fact]
        public async Task TestLookupCpscCom()
        {
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("8.8.8.8"));
            
            var answer = await client.Query(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY), CancellationToken.None);

            Assert.Empty(answer.Answers);
            Assert.True(answer.Header.Truncation);
        }

        [Fact]
        public async Task TestLookupTimeout()
        {
            // Reserved - see https://en.wikipedia.org/wiki/Reserved_IP_addresses
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("192.88.99.0"));
            
            await Assert.ThrowsAsync<DnsClientTimeoutException>(() => client.Query(DnsHeader.CreateQuery("alanedwardes.com"), CancellationToken.None));
        }
    }
}
