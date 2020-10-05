using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
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
        static void Main(string[] args) => DoWork(args).GetAwaiter().GetResult();

        private static async Task DoWork(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile("config.json", true)
                .Build();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var dnsConfiguration = new DnsConfiguration();
            configuration.Bind(dnsConfiguration);

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddSerilog(logger));

            foreach (Uri httpsUpstream in dnsConfiguration.HttpsUpstreams)
            {
                services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = httpsUpstream)
                        .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
            }

            foreach (IPAddress udpUpstream in dnsConfiguration.UdpUpstreams.Select(IPAddress.Parse))
            {
                services.AddSingleton<IDnsClient>(new DnsUdpClient(udpUpstream));
            }

            services.AddHttpClient<DnsRemoteSetFilter>()
                    .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            IServiceProvider provider = services.BuildServiceProvider();

            var selfLogger = provider.GetRequiredService<ILogger<Program>>();

            var remoteFilter = provider.GetRequiredService<DnsRemoteSetFilter>();

            selfLogger.LogInformation("Adding {RemoteBlocklistCount} remote blocklists", dnsConfiguration.RemoteBlocklists.Length);

            foreach (Uri remoteBlockList in dnsConfiguration.RemoteBlocklists)
            {
                _ = remoteFilter.AddRemoteBlockList(remoteBlockList);
            }

            var upstreams = provider.GetServices<IDnsClient>().ToArray();
            if (!upstreams.Any())
            {
                throw new Exception("No upstream DNS servers specified - you must specify at least one");
            }

            selfLogger.LogInformation("Using {UpstreamCount} DNS upstreams", upstreams.Length);

            IDnsClient combinedDnsClient = new DnsRoundRobinClient(upstreams);

            IDnsClient cache = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), combinedDnsClient, new MemoryCache("dns"));

            selfLogger.LogInformation("Adding {AllowListedDomains} domains to explicit allow list", dnsConfiguration.AllowlistedDomains.Length);

            var staticFilter = new DnsDelegateFilter(x => dnsConfiguration.AllowlistedDomains.Contains(x.Host));

            IDnsClient filter = new DnsFilterClient(provider.GetRequiredService<ILogger<DnsFilterClient>>(), new DnsCompositeOrFilter(remoteFilter, staticFilter), cache);

            IDnsServer server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new IPEndPoint(IPAddress.Any, 53), filter);

            await server.Listen(CancellationToken.None);
        }
    }
}
