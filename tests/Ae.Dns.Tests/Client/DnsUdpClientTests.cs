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
        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithCloudFlare(string domain, DnsQueryType type)
        {
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("1.1.1.1"));
            await client.RunQuery(domain, type, type == DnsQueryType.ANY ? DnsResponseCode.NotImp : DnsResponseCode.NoError);
        }

        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithGoogle(string domain, DnsQueryType type)
        {
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("8.8.8.8"));
            await client.RunQuery(domain, type);
        }

        [Fact]
        public async Task TestLookupTimeout()
        {
            // Reserved - see https://en.wikipedia.org/wiki/Reserved_IP_addresses
            using var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), IPAddress.Parse("192.88.99.0"));
            await Assert.ThrowsAsync<DnsClientTimeoutException>(() => client.Query(DnsQueryFactory.CreateQuery("alanedwardes.com"), CancellationToken.None));
        }
    }
}
