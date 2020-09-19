using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Server.Http
{
    public sealed class DnsMiddlewareConfig : IDnsMiddlewareConfig
    {
        public PathString Path { get; set; } = "/dns-query";
    }
}
