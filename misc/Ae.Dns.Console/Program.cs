using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Console
{
    public sealed class DnsConfiguration
    {
        public Uri HttpClientResolver { get; set; } = new Uri("https://1.1.1.1/");
        public Uri[] HttpsUpstreams { get; set; } = new Uri[0];
        public string[] UdpUpstreams { get; set; } = new string[0];
        public Uri[] RemoteBlocklists { get; set; } = new Uri[0];
        public string[] AllowlistedDomains { get; set; } = new string[0];
    }

    class Program
    {
        static void Main(string[] args) => DoWork(args).GetAwaiter().GetResult();

        private static async Task DoWork(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var dnsConfiguration = new DnsConfiguration();
            configuration.Bind(dnsConfiguration);

            const string staticDnsResolver = "StaticResolver";

            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("dns.log", LogEventLevel.Warning)
                .WriteTo.Console()
                .CreateLogger();

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddSerilog(logger));

            services.AddHttpClient(staticDnsResolver, x => x.BaseAddress = dnsConfiguration.HttpClientResolver);

            static DnsDelegatingHandler CreateDnsDelegatingHandler(IServiceProvider serviceProvider)
            {
                var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(staticDnsResolver);
                return new DnsDelegatingHandler(new DnsHttpClient(httpClient));
            }

            foreach (Uri httpsUpstream in dnsConfiguration.HttpsUpstreams)
            {
                services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = httpsUpstream)
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

            var remoteFilter = provider.GetRequiredService<DnsRemoteSetFilter>();

            foreach (Uri remoteBlockList in dnsConfiguration.RemoteBlocklists)
            {
                _ = remoteFilter.AddRemoteBlockList(remoteBlockList);
            }

            var selfLogger = provider.GetRequiredService<ILogger<Program>>();

            var upstreams = provider.GetServices<IDnsClient>().ToArray();
            if (!upstreams.Any())
            {
                throw new Exception("No upstream DNS servers specified");
            }

            selfLogger.LogInformation("Using {UpstreamCount} DNS upstreams", upstreams.Length);

            IDnsClient combinedDnsClient = new DnsRoundRobinClient(upstreams);

            IDnsClient cache = new DnsCachingClient(provider.GetRequiredService<ILogger<DnsCachingClient>>(), combinedDnsClient, new MemoryCache("dns"));

            var staticFilter = new DnsDelegateFilter(x => dnsConfiguration.AllowlistedDomains.Contains(x.Host));

            IDnsClient filter = new DnsFilterClient(provider.GetRequiredService<ILogger<DnsFilterClient>>(), new DnsCompositeOrFilter(remoteFilter, staticFilter), cache);

            IDnsServer server = new DnsUdpServer(provider.GetRequiredService<ILogger<DnsUdpServer>>(), new IPEndPoint(IPAddress.Any, 53), filter);

            await server.Listen(CancellationToken.None);
        }
    }
}
