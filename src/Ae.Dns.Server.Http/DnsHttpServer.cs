using Ae.Dns.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    public sealed class DnsHttpServer : IDnsServer
    {
        private readonly IDnsClient _dnsClient;
        private readonly Func<IWebHostBuilder, IWebHostBuilder> _configureBuilder;

        public DnsHttpServer(IDnsClient dnsClient, Func<IWebHostBuilder, IWebHostBuilder> configureBuilder)
        {
            _dnsClient = dnsClient;
            _configureBuilder = configureBuilder;
        }

        public void Dispose()
        {
        }

        public async Task Listen(CancellationToken token)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(x => 
                {
                    _configureBuilder(x);
                    x.UseStartup<DnsStartup>();
                    x.ConfigureServices(y => y.AddSingleton(_dnsClient));
                });

            await builder.Build().RunAsync(token);
        }
    }
}
