using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Server.Http
{
    /// <summary>
    /// Describes configuration for the <see cref="DnsMiddleware"/>.
    /// </summary>
    public interface IDnsMiddlewareConfig
    {
        /// <summary>
        /// The path to serve queries from, for example /dns-query
        /// </summary>
        public PathString Path { get; }
    }
}
