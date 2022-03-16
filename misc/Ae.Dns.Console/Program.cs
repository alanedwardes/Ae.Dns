using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"), true)
                .Build();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var dnsConfiguration = new DnsConfiguration();
            configuration.Bind(dnsConfiguration);

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddSerilog(logger));

            const string staticDnsResolverHttpClient = "StaticResolver";

            var memoryCache = new MemoryCache("dns");

            services.AddHttpClient(staticDnsResolverHttpClient, x => x.BaseAddress = new Uri("https://1.1.1.1/"));
            services.AddSingleton<ObjectCache>(memoryCache);

            static DnsDelegatingHandler CreateDnsDelegatingHandler(IServiceProvider serviceProvider)
            {
                var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(staticDnsResolverHttpClient);
                return new DnsDelegatingHandler(new DnsCachingClient(new DnsHttpClient(httpClient), serviceProvider.GetRequiredService<ObjectCache>()));
            }

            foreach (Uri httpsUpstream in dnsConfiguration.HttpsUpstreams)
            {
                services.AddHttpClient<IDnsClient, DnsHttpClient>(httpsUpstream.ToString(), x => x.BaseAddress = httpsUpstream)
                        .AddHttpMessageHandler(CreateDnsDelegatingHandler)
                        .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
            }

            foreach (IPAddress udpUpstream in dnsConfiguration.UdpUpstreams.Select(IPAddress.Parse))
            {
                services.AddSingleton<IDnsClient>(new DnsUdpClient(udpUpstream));
            }

            services.AddHttpClient<DnsRemoteSetFilter>()
                    .AddHttpMessageHandler(CreateDnsDelegatingHandler)
                    .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            IServiceProvider provider = services.BuildServiceProvider();

            var selfLogger = provider.GetRequiredService<ILogger<Program>>();

            selfLogger.LogInformation("Working directory is {WorkingDirectory}", Directory.GetCurrentDirectory());

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

            IDnsClient cache = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), combinedDnsClient, memoryCache);

            selfLogger.LogInformation("Adding {AllowListedDomains} domains to explicit allow list", dnsConfiguration.AllowlistedDomains.Length);

            var allowListFilter = new DnsDelegateFilter(x => dnsConfiguration.AllowlistedDomains.Contains(x.Host));

            selfLogger.LogInformation("Adding {DisallowedDomainPrefixes} domain suffixes to explicit disallow list", dnsConfiguration.DisallowedDomainSuffixes.Length);

            var suffixFilter = new DnsDomainSuffixFilter(dnsConfiguration.DisallowedDomainSuffixes);

            // The domain must pass all of these filters to be allowed
            var denyFilter = new DnsCompositeAndFilter(remoteFilter, suffixFilter);

            // The domain must pass one of these filters to be allowed
            var compositeFilter = new DnsCompositeOrFilter(denyFilter, allowListFilter);

            IDnsClient filter = new DnsFilterClient(provider.GetRequiredService<ILogger<DnsFilterClient>>(), compositeFilter, cache);

            IDnsServer server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new IPEndPoint(IPAddress.Any, 53), filter);

            // Add a very basic stats panel
            var builder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup<Startup>());

            await Task.WhenAll(
                builder.Build().RunAsync(CancellationToken.None),
                server.Listen(CancellationToken.None)
            );
        }
    }
}
