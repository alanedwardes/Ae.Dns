using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    public class DnsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDnsClient _dnsClient;

        public DnsMiddleware(RequestDelegate next, IDnsClient dnsClient)
        {
            _next = next;
            _dnsClient = dnsClient;
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
}
