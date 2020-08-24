using Ae.DnsResolver.Client;
using Ae.DnsResolver.Client.Exceptions;
using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;
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

            using (var udpClient = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                udpClient.Connect("1.1.1.1", 53);

                using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), udpClient, "test"))
                {
                    result = await client.LookupRaw("alanedwardes.com", DnsQueryType.A);
                }
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Length);
        }

        [Fact]
        public async Task TestLookupCpscCom()
        {
            byte[] result;

            using (var udpClient = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                udpClient.Connect("8.8.8.8", 53);

                using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), udpClient, "test"))
                {
                    result = await client.LookupRaw("cpsc.gov", DnsQueryType.ANY);
                }
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);
            Assert.Empty(answer.Answers);
            Assert.True(answer.Header.Truncation);
        }

        [Fact]
        public async Task TestLookupTimeout()
        {
            using (var udpClient = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                // Reserved - see https://en.wikipedia.org/wiki/Reserved_IP_addresses
                udpClient.Connect("192.88.99.0", 53);

                using (var client = new DnsUdpClient(new NullLogger<DnsUdpClient>(), udpClient, "test"))
                {
                    await Assert.ThrowsAsync<DnsClientTimeoutException>(() => client.LookupRaw("alanedwardes.com", DnsQueryType.A));
                }
            }
        }
    }
}
