using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Ae.Dns.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddHttpClient("Crawler", x=> x.Timeout = TimeSpan.FromSeconds(5));

            services.AddSingleton<Crawler>();

            services.AddLogging(x => x.AddConsole());

            services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = new Uri("https://dns.google/"));

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<Crawler>().Crawl().GetAwaiter().GetResult();
        }
    }
}
