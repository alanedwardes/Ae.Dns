using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Server.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
            IDnsFilter dnsFilter = new DnsDelegateFilter(x => true);

            var server = new DnsHttpServer(null, dnsClient, dnsFilter);

            server.Listen(CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    public sealed class DnsHttpServer : IDnsServer
    {
        private readonly ILogger<DnsHttpServer> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly IDnsFilter _dnsFilter;

        public DnsHttpServer(IDnsClient dnsClient, IDnsFilter dnsFilter)
            : this(new NullLogger<DnsHttpServer>(), dnsClient, dnsFilter)
        {
        }

        public DnsHttpServer(ILogger<DnsHttpServer> logger, IDnsClient dnsClient, IDnsFilter dnsFilter)
        {
            _logger = logger;
            _dnsClient = dnsClient;
            _dnsFilter = dnsFilter;
        }

        public void Dispose()
        {
        }

        public async Task Listen(CancellationToken token)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(x => x.UseStartup<DnsStartup>())
                .ConfigureServices(x => x.AddSingleton(_dnsClient).AddSingleton(_dnsFilter));

            await builder.Build().RunAsync(token);
        }
    }

    public class DnsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDnsClient _dnsClient;
        private readonly IDnsFilter _dnsFilter;

        public DnsMiddleware(RequestDelegate next, IDnsClient dnsClient, IDnsFilter dnsFilter)
        {
            _next = next;
            _dnsClient = dnsClient;
            _dnsFilter = dnsFilter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ms = await context.Request.BodyReader.ReadAsync(context.RequestAborted);

            var buffer = ms.Buffer.ToArray();

            var header = buffer.FromBytes<DnsHeader>();

            var answer = await _dnsClient.Query(header, context.RequestAborted);

            context.Response.Headers.Add("Content-Type", new StringValues("application/dns-message"));

            await context.Response.BodyWriter.WriteAsync(answer.ToBytes(), context.RequestAborted);
        }
    }

    public class DnsStartup
    {
        public void Configure(IApplicationBuilder app) => app.UseMiddleware<DnsMiddleware>();
    }
}
