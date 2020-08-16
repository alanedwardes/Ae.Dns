using Ae.DnsResolver.Client;
using Ae.DnsResolver.Repository;
using Ae.DnsResolver.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            DoWork().GetAwaiter().GetResult();
        }

        private static async Task DoWork()
        {
            var services = new ServiceCollection();
            services.AddLogging(x =>
            {
                x.AddConsole();
                x.SetMinimumLevel(LogLevel.Trace);
            });
            var provider = services.BuildServiceProvider();

            var cloudFlare = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), new UdpClient("1.1.1.1", 53));
            var google1 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), new UdpClient("8.8.8.8", 53));
            var google2 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), new UdpClient("8.8.4.4", 53));

            var filter = new DnsRemoteSetFilter(provider.GetRequiredService<ILogger<DnsRemoteSetFilter>>());

            _ = filter.AddRemoteList(new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts"));
            _ = filter.AddRemoteList(new Uri("https://mirror1.malwaredomains.com/files/justdomains"));
            _ = filter.AddRemoteList(new Uri("https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt"));

            var combinedDnsClient = new DnsCompositeClient(cloudFlare, google1, google2);

            var repository = new DnsRepository(provider.GetRequiredService<ILogger<DnsRepository>>(), combinedDnsClient, new MemoryCache("dns"), filter);

            var server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new UdpClient(53), repository);

            await server.Recieve(CancellationToken.None);
        }
    }
}
