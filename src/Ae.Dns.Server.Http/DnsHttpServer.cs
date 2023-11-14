using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    /// <summary>
    /// Offers DNS over HTTPS (DoH) functionality using ASP.NET core middleware <see cref="DnsMiddleware"/>.
    /// </summary>
    public sealed class DnsHttpServer : IDnsServer
    {
        private readonly IDnsClient _dnsClient;
        private readonly Func<IWebHostBuilder, IWebHostBuilder> _configureBuilder;
        private readonly IDnsMiddlewareConfig _middlewareConfig;

        /// <summary>
        /// Construct a new <see cref="DnsHttpServer"/>.
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="configureBuilder"></param>
        /// <param name="middlewareConfig"></param>
        public DnsHttpServer(IDnsClient dnsClient, Func<IWebHostBuilder, IWebHostBuilder> configureBuilder, IDnsMiddlewareConfig middlewareConfig)
        {
            _dnsClient = dnsClient;
            _configureBuilder = configureBuilder;
            _middlewareConfig = middlewareConfig;
        }

        /// <summary>
        /// Construct a new <see cref="DnsHttpServer"/>.
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="configureBuilder"></param>
        public DnsHttpServer(IDnsClient dnsClient, Func<IWebHostBuilder, IWebHostBuilder> configureBuilder)
            : this(dnsClient, configureBuilder, new DnsMiddlewareConfig())
        {
        }

        /// <summary>
        /// Construct a new <see cref="DnsHttpServer"/>.
        /// </summary>
        /// <param name="dnsClient"></param>
        public DnsHttpServer(IDnsClient dnsClient)
            : this(dnsClient, x => x, new DnsMiddlewareConfig())
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task Listen(CancellationToken token)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(x => 
                {
                    _configureBuilder(x);
                    x.UseStartup<DnsStartup>();
                    x.ConfigureServices(y => y.AddSingleton(_dnsClient).AddSingleton(_middlewareConfig));
                });

            await builder.Build().RunAsync(token);
        }
    }
}
