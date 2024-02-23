using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Client.Lookup;
using Ae.Dns.Metrics.InfluxDb;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Zone;
using Ae.Dns.Server;
using Ae.Dns.Server.Http;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete

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

            var dnsConfiguration = new DnsConfiguration();
            configuration.Bind(dnsConfiguration);

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole());
            services.Configure<DnsUdpServerOptions>(configuration.GetSection("udpServer"));
            services.Configure<DnsTcpServerOptions>(configuration.GetSection("tcpServer"));

            const string staticDnsResolverHttpClient = "StaticResolver";
            services.AddHttpClient(staticDnsResolverHttpClient, x => x.BaseAddress = new Uri("https://1.1.1.1/"))
                    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            services.AddSingleton<ObjectCache>(new MemoryCache("ResolverCache"));

            static DnsDelegatingHandler CreateDnsDelegatingHandler(IServiceProvider serviceProvider)
            {
                var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(staticDnsResolverHttpClient);
                var cachingClient = ActivatorUtilities.CreateInstance<DnsCachingClient>(serviceProvider, new DnsHttpClient(httpClient));
                return new DnsDelegatingHandler(cachingClient);
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

            IDnsClient[] upstreams = provider.GetServices<IDnsClient>().ToArray();
            if (!upstreams.Any())
            {
                throw new Exception("No upstream DNS servers specified - you must specify at least one");
            }

            selfLogger.LogInformation("Using {UpstreamCount} DNS upstreams", upstreams.Length);

            IDnsClient queryClient;
            if (dnsConfiguration.ClientGroups.Any())
            {
                IDnsClient FindUpstreamByTag(string tag)
                {
                    var upstream = upstreams.SingleOrDefault(x => string.Equals(x.ToString(), tag));
                    if (upstream == null)
                    {
                        throw new Exception($"DNS upstream client with tag {tag} not found. Available tags: {string.Join(", ", upstreams.Select(x => x.ToString()))}");
                    }
                    return upstream;
                }

                var groupRacerOptions = new DnsGroupRacerClientOptions
                {
                    DnsClientGroups = dnsConfiguration.ClientGroups.ToDictionary(x => x.Key, x => (IReadOnlyList<IDnsClient>)x.Value.Select(y => FindUpstreamByTag(y)).ToArray())
                };

                queryClient = ActivatorUtilities.CreateInstance<DnsGroupRacerClient>(provider, Options.Create(groupRacerOptions));
            }
            else
            {
                queryClient = ActivatorUtilities.CreateInstance<DnsRacerClient>(provider, upstreams.AsEnumerable());
            }

            queryClient = ActivatorUtilities.CreateInstance<DnsRebindMitigationClient>(provider, queryClient);

            ObjectCache mainCache = new MemoryCache("MainCache");

            queryClient = ActivatorUtilities.CreateInstance<DnsCachingClient>(provider, queryClient, mainCache);

            selfLogger.LogInformation("Adding {AllowListedDomains} domains to explicit allow list", dnsConfiguration.AllowlistedDomains.Length);

            var allowListFilter = new DnsDelegateFilter(x => dnsConfiguration.AllowlistedDomains.Contains(x.Header.Host.ToString()));

            selfLogger.LogInformation("Adding {DisallowedDomainPrefixes} domain suffixes to explicit disallow list", dnsConfiguration.DisallowedDomainSuffixes.Length);

            var suffixFilter = new DnsDomainSuffixFilter(dnsConfiguration.DisallowedDomainSuffixes);

            var networkFilter = new DnsLocalNetworkQueryFilter();

            var queryTypeFilter = new DnsQueryTypeFilter(dnsConfiguration.DisallowedQueryTypes.Select(Enum.Parse<DnsQueryType>));

            // The domain must pass all of these filters to be allowed
            var denyFilter = new DnsCompositeAndFilter(remoteFilter, suffixFilter, networkFilter, queryTypeFilter);

            // The domain must pass one of these filters to be allowed
            var compositeFilter = new DnsCompositeOrFilter(denyFilter, allowListFilter);

            queryClient = ActivatorUtilities.CreateInstance<DnsFilterClient>(provider, compositeFilter, queryClient);

            var metricsBuilder = new MetricsBuilder();

            var metrics = new MetricsBuilder().OutputMetrics.AsPrometheusPlainText().Build();

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
                staticLookupSources.Add(ActivatorUtilities.CreateInstance<HostsFileDnsLookupSource>(provider, new FileInfo(hostFile)));
            }

            IDnsClient updateClient = DnsNotImplementedClient.Instance;

            if (dnsConfiguration.UpdateZoneName != null)
            {
                var dnsZone = new SingleWriterDnsZone();
                var zoneFile = $"{dnsConfiguration.UpdateZoneName}.zone";

                if (File.Exists(zoneFile))
                {
                    // Load the existing zone file
                    DnsZoneSerializer.DeserializeZone(dnsZone, File.ReadAllText(zoneFile));
                    selfLogger.LogInformation("Loaded {RecordCount} records from zone file {ZoneFile}", dnsZone.Records.Count, zoneFile);
                }
                else
                {
                    // Set some defaults for the new zone
                    dnsZone.Origin = dnsConfiguration.UpdateZoneName;
                    dnsZone.DefaultTtl = TimeSpan.FromHours(1);
                    selfLogger.LogInformation("Created new zone backed by {ZoneFile}", zoneFile);
                }

                // Update the file when the zone is updated
                dnsZone.ZoneUpdated = async zone => await File.WriteAllTextAsync(zoneFile, DnsZoneSerializer.SerializeZone(zone));

                // Replace the clients with clients for the zone
                queryClient = new DnsZoneClient(queryClient, dnsZone);
                updateClient = ActivatorUtilities.CreateInstance<DnsZoneUpdateClient>(provider, dnsZone);

                // Add the zone file as a source of automatic reverse lookups
                staticLookupSources.Add(new DnsZoneLookupSource(dnsZone));
            }

            if (staticLookupSources.Count > 0)
            {
                queryClient = new DnsStaticLookupClient(queryClient, staticLookupSources.ToArray());
            }

            // Route query and update operations as appropriate
            IDnsClient operationClient = new DnsOperationRouter(new Dictionary<DnsOperationCode, IDnsClient>
            {
                { DnsOperationCode.QUERY, queryClient },
                { DnsOperationCode.UPDATE, updateClient }
            });

            // Track metrics last
            operationClient = new DnsMetricsClient(operationClient);
            operationClient = new DnsAppMetricsClient(metrics, operationClient);

            // Create a "raw" client which deals with buffers directly
            IDnsRawClient rawClient = ActivatorUtilities.CreateInstance<DnsRawClient>(provider, operationClient);

            // Create a dormant capture client for debugging purposes
            DnsCaptureRawClient captureRawClient = ActivatorUtilities.CreateInstance<DnsCaptureRawClient>(provider, rawClient);

            // Create two servers, TCP and UDP to serve answers
            IDnsServer tcpServer = ActivatorUtilities.CreateInstance<DnsTcpServer>(provider, captureRawClient);
            IDnsServer udpServer = ActivatorUtilities.CreateInstance<DnsUdpServer>(provider, captureRawClient);

            // Add a very basic stats panel
            var builder = Host.CreateDefaultBuilder()
                .ConfigureMetrics(metrics)
                .UseMetrics(options =>
                {
                    options.EndpointOptions = endpointsOptions =>
                    {
                        endpointsOptions.MetricsTextEndpointOutputFormatter = metrics.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                    };
                })
                .ConfigureLogging(x =>
                {
                    x.AddConsole();
                    x.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                    x.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
                })
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseStartup<Startup>();
                    webHostBuilder.UseConfiguration(configuration.GetSection("statsServer"));
                })
                .ConfigureServices(x =>
                {
                    x.AddSingleton(captureRawClient);
                    x.AddSingleton(mainCache);
                    x.AddSingleton<IDnsMiddlewareConfig>(new DnsMiddlewareConfig());
                    x.AddSingleton(operationClient);
                });

            await Task.WhenAll(
                builder.Build().RunAsync(CancellationToken.None),
                ReportStats(CancellationToken.None),
                udpServer.Listen(CancellationToken.None),
                tcpServer.Listen(CancellationToken.None)
            );
        }
    }
}
