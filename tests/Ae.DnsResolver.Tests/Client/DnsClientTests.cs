using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Ae.DnsResolver.Tests.Client
{
    public class DnsClientTests
    {
        [Fact]
        public async Task TestLookup()
        {
            byte[] result;

            using (var udpClient = new UdpClient("1.1.1.1", 53))
            using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), udpClient, "test"))
            {
                result = await client.LookupRaw("alanedwardes.com", DnsQueryType.A);
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Length);
        }
    }
}
