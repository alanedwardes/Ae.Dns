using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.IO;
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

#if NETCOREAPP2_1
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
            ms.Position = 0;
            var buffer = ms.ToArray();
#else
            var ms = await context.Request.BodyReader.ReadAsync(context.RequestAborted);
            var buffer = ms.Buffer.ToArray();
#endif

            var header = DnsByteExtensions.FromBytes<DnsMessage>(buffer);

            var answer = await _dnsClient.Query(header, context.RequestAborted);

            context.Response.Headers.Add("Content-Type", new StringValues(DnsMessageType));

            var answerBuffer = DnsByteExtensions.AllocateAndWrite(answer);

#if NETCOREAPP2_1
            using var ms2 = new MemoryStream(answerBuffer.ToArray());
            await ms2.CopyToAsync(context.Response.Body);
#else
            await context.Response.BodyWriter.WriteAsync(answerBuffer.ToArray(), context.RequestAborted);
#endif
        }
    }
}
