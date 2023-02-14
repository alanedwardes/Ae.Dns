#if !NETCOREAPP2_1
using Ae.Dns.Client;
using Ae.Dns.Client.Polly;
using Ae.Dns.Protocol;
using Ae.Dns.Server.Http;
using Microsoft.AspNetCore.Hosting;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Server
{
    public class DnsHttpServerTests
    {
        public static IPEndPoint GenerateEndPoint() => new IPEndPoint(IPAddress.Loopback, new Random().Next(1024, IPEndPoint.MaxPort));

        [Fact]
        public async Task TestStartupShutdown()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            using var server = new DnsHttpServer(new DnsUdpClient(IPAddress.Loopback), x => x.ConfigureKestrel(y => y.Listen(GenerateEndPoint())));

            await server.Listen(tokenSource.Token);
        }

        [Fact]
        public async Task TestQuery()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var endpoint = GenerateEndPoint();

            // Create a real upstream DNS client to serve the request (todo: mock this)
            using var upstream = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

            // Retry 6 times
            using var retry = new DnsPollyClient(upstream, Policy<DnsMessage>.Handle<Exception>().WaitAndRetryAsync(6, x => TimeSpan.FromSeconds(x)));

            // Create a loopback server
            using var server = new DnsHttpServer(retry, x => x.ConfigureKestrel(y => y.Listen(endpoint)));

            // Start recieving
            using var receiveTask = server.Listen(tokenSource.Token);

            // Create a loopback DNS client to talk to the server
            using var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri($"http://{endpoint.Address}:{endpoint.Port}") });

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
    }
}
#endif
