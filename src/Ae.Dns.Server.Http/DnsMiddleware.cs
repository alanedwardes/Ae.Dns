using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    public class DnsMiddleware
    {
        private const string DnsMessageType = "application/dns-message";
        private readonly RequestDelegate _next;
        private readonly IDnsClient _dnsClient;

        public DnsMiddleware(RequestDelegate next, IDnsClient dnsClient)
        {
            _next = next;
            _dnsClient = dnsClient;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(new PathString("/dns-query")))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("Accepts", out StringValues accepts) || !accepts.Contains(DnsMessageType))
            {
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                return;
            }

            var ms = await context.Request.BodyReader.ReadAsync(context.RequestAborted);

            var buffer = ms.Buffer.ToArray();

            var header = buffer.FromBytes<DnsHeader>();

            var answer = await _dnsClient.Query(header, context.RequestAborted);

            context.Response.Headers.Add("Content-Type", new StringValues(DnsMessageType));

            await context.Response.BodyWriter.WriteAsync(answer.ToBytes(), context.RequestAborted);
        }
    }
}
