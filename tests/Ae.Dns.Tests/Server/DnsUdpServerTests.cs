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
        [Fact]
        public async Task TestStartupShutdown()
        {
            var random = new Random();

            using var server = new DnsUdpServer(new IPEndPoint(IPAddress.Loopback, random.Next(1024, ushort.MaxValue)), null, null);

            var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            await server.Recieve(tokenSource.Token);
        }
    }
}
