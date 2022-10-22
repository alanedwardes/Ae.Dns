using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Client.Lookup;
using Ae.Dns.Metrics.InfluxDb;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using App.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
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

            services.AddHttpClient(staticDnsResolverHttpClient, x => x.BaseAddress = new Uri("https://1.1.1.1/"))
                    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            services.AddSingleton<ObjectCache>(new MemoryCache("ResolverCache"));

            static DnsDelegatingHandler CreateDnsDelegatingHandler(IServiceProvider serviceProvider)
            {
                var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(staticDnsResolverHttpClient);
                return new DnsDelegatingHandler(new DnsCachingClient(new DnsHttpClient(httpClient), serviceProvider.GetRequiredService<ObjectCache>()));
            }

            foreach (Uri httpsUpstream in dnsConfiguration.HttpsUpstreams)
            {
                services.AddHttpClient<IDnsClient, DnsHttpClient>(Guid.NewGuid().ToString(), x => x.BaseAddress = httpsUpstream)
                        .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                        .AddHttpMessageHandler(CreateDnsDelegatingHandler);
            }

            foreach (IPAddress udpUpstream in dnsConfiguration.UdpUpstreams.Select(IPAddress.Parse))
            {
                services.AddSingleton<IDnsClient>(new DnsUdpClient(udpUpstream));
            }

            services.AddHttpClient<DnsRemoteSetFilter>()
                    .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                    .AddHttpMessageHandler(CreateDnsDelegatingHandler);

            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

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

            IDnsClient dnsClient = new DnsRacerClient(provider.GetRequiredService<ILogger<DnsRacerClient>>(), upstreams);

            dnsClient = new DnsRebindMitigationClient(provider.GetRequiredService<ILogger<DnsRebindMitigationClient>>(), dnsClient);

            dnsClient = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), dnsClient, new MemoryCache("MainCache"));

            selfLogger.LogInformation("Adding {AllowListedDomains} domains to explicit allow list", dnsConfiguration.AllowlistedDomains.Length);

            var allowListFilter = new DnsDelegateFilter(x => dnsConfiguration.AllowlistedDomains.Contains(x.Header.Host));

            selfLogger.LogInformation("Adding {DisallowedDomainPrefixes} domain suffixes to explicit disallow list", dnsConfiguration.DisallowedDomainSuffixes.Length);

            var suffixFilter = new DnsDomainSuffixFilter(dnsConfiguration.DisallowedDomainSuffixes);

            var networkFilter = new DnsLocalNetworkQueryFilter();

            // The domain must pass all of these filters to be allowed
            var denyFilter = new DnsCompositeAndFilter(remoteFilter, suffixFilter, networkFilter);

            // The domain must pass one of these filters to be allowed
            var compositeFilter = new DnsCompositeOrFilter(denyFilter, allowListFilter);

            dnsClient = new DnsFilterClient(provider.GetRequiredService<ILogger<DnsFilterClient>>(), compositeFilter, dnsClient);

            var metricsBuilder = new MetricsBuilder();

            if (dnsConfiguration.InfluxDbMetrics != null)
            {
                metricsBuilder.Report.ToInfluxDb(options =>
                {
                    options.InfluxDb.BaseUri = dnsConfiguration.InfluxDbMetrics.BaseUri;
                    options.InfluxDb.Organization = dnsConfiguration.InfluxDbMetrics.Organization;
                    options.InfluxDb.Bucket = dnsConfiguration.InfluxDbMetrics.Bucket;
                    options.InfluxDb.Token = dnsConfiguration.InfluxDbMetrics.Token;
                });
            }

            var metrics = metricsBuilder.Build();

            async Task ReportStats(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.WhenAll(metrics.ReportRunner.RunAllAsync(token));
                    }
                    catch (Exception ex)
                    {
                        selfLogger.LogWarning(ex, "Unable to report statistics");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            }

            var staticLookupSources = new List<IDnsLookupSource>();

            foreach (var hostFile in dnsConfiguration.HostFiles)
            {
                staticLookupSources.Add(new HostsFileDnsLookupSource(provider.GetRequiredService<ILogger<HostsFileDnsLookupSource>>(), new FileInfo(hostFile)));
            }

            if (!string.IsNullOrWhiteSpace(dnsConfiguration.DhcpdLeasesFile))
            {
                staticLookupSources.Add(new DhcpdLeasesDnsLookupSource(provider.GetRequiredService<ILogger<DhcpdLeasesDnsLookupSource>>(), new FileInfo(dnsConfiguration.DhcpdLeasesFile), dnsConfiguration.DhcpdLeasesHostnameSuffix));
            }

            if (staticLookupSources.Count > 0)
            {
                dnsClient = new DnsStaticLookupClient(dnsClient, staticLookupSources.ToArray());
            }

            // Track metrics last
            dnsClient = new DnsMetricsClient(dnsClient);
            dnsClient = new DnsAppMetricsClient(metrics, dnsClient);

            IDnsServer server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new IPEndPoint(IPAddress.Any, 53), new DnsSingleBufferClient(dnsClient));

            // Add a very basic stats panel
            var builder = Host.CreateDefaultBuilder()
                .ConfigureLogging(x => x.ClearProviders().AddSerilog(logger))
                .ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup<Startup>());

            await Task.WhenAll(
                builder.Build().RunAsync(CancellationToken.None),
                ReportStats(CancellationToken.None),
                server.Listen(CancellationToken.None)
            );
        }
    }
}
