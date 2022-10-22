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
        public static IPEndPoint GenerateEndPoint() => new IPEndPoint(IPAddress.Loopback, new Random().Next(1024, IPEndPoint.MaxPort));

        [Fact]
        public async Task TestStartupShutdown()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            using var server = new DnsUdpServer(GenerateEndPoint(), null);

            await server.Listen(tokenSource.Token);
        }

        [Fact]
        public async Task TestQuery()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var endpoint = GenerateEndPoint();

            // Create a real upstream DNS client to serve the request (todo: mock this)
            using var upstream = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

            // Create a loopback server
            using var server = new DnsUdpServer(endpoint, upstream);

            // Start recieving
            using var receiveTask = server.Listen(tokenSource.Token);

            // Create a loopback DNS client to talk to the server
            using var client = new DnsUdpClient(endpoint);

            try
            {
                var query = DnsQueryFactory.CreateQuery("google.com");

                // Send a DNS request to the server, verify the results
                var response = await client.Query(query, tokenSource.Token);

                Assert.Equal(query.Header.Id, response.Header.Id);
                Assert.Equal(query.Header.Host, response.Header.Host);
                Assert.True(response.Answers.Count > 0);
            }
            finally
            {
                tokenSource.Cancel();
                await receiveTask;
            }
        }

        [Fact]
        public async Task TestQuery1()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var endpoint = GenerateEndPoint();

            // Create a real upstream DNS client to serve the request (todo: mock this)
            using var upstream = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

            // Create a loopback server
            using var server = new DnsUdpServer(endpoint, upstream);

            // Start recieving
            using var receiveTask = server.Listen(tokenSource.Token);

            // Create a loopback DNS client to talk to the server
            using var client = new DnsUdpClient(endpoint);

            try
            {
                var query = DnsQueryFactory.CreateQuery("google.com", Dns.Protocol.Enums.DnsQueryType.HTTPS);

                // Send a DNS request to the server, verify the results
                var response = await client.Query(query, tokenSource.Token);

                Assert.Equal(query.Header.Id, response.Header.Id);
                Assert.Equal(query.Header.Host, response.Header.Host);
                Assert.True(response.Answers.Count > 0);
            }
            finally
            {
                tokenSource.Cancel();
                await receiveTask;
            }
        }
    }
}
