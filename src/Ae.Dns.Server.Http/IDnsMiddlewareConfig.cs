using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Server.Http
{
    public interface IDnsMiddlewareConfig
    {
        public PathString Path { get; }
    }
}
