using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Server.Http
{
    /// <inheritdoc/>
    public sealed class DnsMiddlewareConfig : IDnsMiddlewareConfig
    {
        /// <inheritdoc/>
        public PathString Path { get; set; } = "/dns-query";
    }
}
