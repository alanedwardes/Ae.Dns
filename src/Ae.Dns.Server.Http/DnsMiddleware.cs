using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ae.Dns.Server.Http
{
    public sealed class DnsMiddleware
    {
        private const string DnsMessageType = "application/dns-message";
        private readonly RequestDelegate _next;
        private readonly IDnsClient _dnsClient;
        private readonly IDnsMiddlewareConfig _middlewareConfig;

        public DnsMiddleware(RequestDelegate next, IDnsClient dnsClient, IDnsMiddlewareConfig middlewareConfig)
        {
            _next = next;
            _dnsClient = dnsClient;
            _middlewareConfig = middlewareConfig;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != _middlewareConfig.Path)
            {
                await _next(context);
                return;
            }

            if (context.Request.GetTypedHeaders().Accept.All(x => x.MediaType != DnsMessageType))
            {
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                return;
            }

            var ms = await context.Request.BodyReader.ReadAsync(context.RequestAborted);

            var buffer = ms.Buffer.ToArray();

            var header = DnsByteExtensions.FromBytes<DnsMessage>(buffer);

            var answer = await _dnsClient.Query(header, context.RequestAborted);

            context.Response.Headers.Add("Content-Type", new StringValues(DnsMessageType));

            await context.Response.BodyWriter.WriteAsync(DnsByteExtensions.ToBytes(answer), context.RequestAborted);
        }
    }
}
