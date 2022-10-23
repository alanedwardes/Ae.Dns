using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Ae.Dns.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddHttpClient("Crawler");

            services.AddSingleton<Crawler>();

            services.AddLogging(x => x.AddConsole());

            services.AddSingleton<IDnsClient>(x =>
            {
                return new DnsUdpClient(x.GetRequiredService<ILogger<DnsUdpClient>>(), IPAddress.Parse("8.8.8.8"));
            });

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<Crawler>().Crawl().GetAwaiter().GetResult();
        }
    }
}
