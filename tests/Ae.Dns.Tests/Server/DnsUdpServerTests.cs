using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Server
{
    public class DnsUdpServerTests
    {
        public static int GeneratePort() => new Random().Next(1024, IPEndPoint.MaxPort);

        [Fact]
        public async Task TestStartupShutdown()
        {
            using var server = new DnsUdpServer(new IPEndPoint(IPAddress.Loopback, GeneratePort()), null, null);

            var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            await server.Recieve(tokenSource.Token);
        }

        [Fact]
        public async Task TestQuery()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, GeneratePort());

            using var upstream = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
            using var server = new DnsUdpServer(endpoint, upstream, new DnsDelegateFilter(x => true));

            var tokenSource = new CancellationTokenSource();

            var recieveTask = server.Recieve(tokenSource.Token);

            using var client = new DnsUdpClient(endpoint);

            try
            {
                var query = DnsHeader.CreateQuery("google.com");

                var response = await client.Query(query, CancellationToken.None);

                Assert.Equal(query.Id, response.Header.Id);
                Assert.Equal(query.Host, response.Header.Host);
                Assert.True(response.Answers.Count > 0);
            }
            finally
            {
                tokenSource.Cancel();
                await recieveTask;
            }
        }
    }
}
