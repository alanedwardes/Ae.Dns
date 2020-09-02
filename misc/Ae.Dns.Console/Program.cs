using Ae.Dns.Client;
using Ae.Dns.Server;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose)
                .WriteTo.File("dns.log", LogEventLevel.Warning)
                .WriteTo.AWSSeriLog(new AWSLoggerConfig{LogGroup = "DnsResolverConsole"}, null, new JsonFormatter())
                .CreateLogger();

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddSerilog(logger));
            var provider = services.BuildServiceProvider();

            var cloudFlareSecure = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://cloudflare-dns.com/") });
            var cloudFlare1 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), IPAddress.Parse("1.1.1.1"));
            var cloudFlare2 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), IPAddress.Parse("1.0.0.1"));
            var googleSecure = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });
            var google1 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), IPAddress.Parse("8.8.8.8"));
            var google2 = new DnsUdpClient(provider.GetRequiredService<ILogger<DnsUdpClient>>(), IPAddress.Parse("8.8.4.4"));

            var filter = new DnsRemoteSetFilter(provider.GetRequiredService<ILogger<DnsRemoteSetFilter>>());

            _ = filter.AddRemoteBlockList(new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts"));
            _ = filter.AddRemoteBlockList(new Uri("https://mirror1.malwaredomains.com/files/justdomains"));
            _ = filter.AddRemoteBlockList(new Uri("https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt"));

            var combinedDnsClient = new DnsRoundRobinClient(cloudFlare1, cloudFlare2, google1, google2);

            var repository = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), combinedDnsClient, new MemoryCache("dns"));

            var server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new UdpClient(53), repository, filter);

            await server.Recieve(CancellationToken.None);
        }
    }
}
