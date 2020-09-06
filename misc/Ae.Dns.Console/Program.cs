using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Console
{
    class Program
    {
        static void Main() => DoWork().GetAwaiter().GetResult();

        private static async Task DoWork()
        {
            var services = new ServiceCollection();
            services.AddLogging(x =>
            {
                x.AddConsole();
                x.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddHttpClient<IDnsClient, DnsHttpClient>("CloudFlare", x => x.BaseAddress = new Uri("https://cloudflare-dns.com/"))
                    .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddHttpClient<IDnsClient, DnsHttpClient>("Google", x => x.BaseAddress = new Uri("https://dns.google/"))
                    .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddDnsClient();

            var provider = services.BuildServiceProvider();

            var socketClientFactory = provider.GetRequiredService<IDnsSocketClientFactory>();

            var udpClients = new[]
            {
                socketClientFactory.CreateUdpClient(IPAddress.Parse("1.1.1.1")),
                socketClientFactory.CreateUdpClient(IPAddress.Parse("1.0.0.1")),
                socketClientFactory.CreateUdpClient(IPAddress.Parse("8.8.8.8")),
                socketClientFactory.CreateUdpClient(IPAddress.Parse("8.8.4.4"))
            };

            var httpClients = provider.GetServices<IDnsClient>();

            var filter = new DnsRemoteSetFilter(provider.GetRequiredService<ILogger<DnsRemoteSetFilter>>());

            _ = filter.AddRemoteBlockList(new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts"));
            _ = filter.AddRemoteBlockList(new Uri("https://mirror1.malwaredomains.com/files/justdomains"));
            _ = filter.AddRemoteBlockList(new Uri("https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt"));

            var combinedDnsClient = new DnsRoundRobinClient(httpClients.Concat(udpClients));

            var filteringDnsClient = new DnsFilterClient(provider.GetRequiredService<ILogger<DnsFilterClient>>(), filter, combinedDnsClient);

            var cache = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), filteringDnsClient, new MemoryCache("dns"));

            using var server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new IPEndPoint(IPAddress.Any, 53), cache);

            await server.Listen(CancellationToken.None);
        }
    }
}
