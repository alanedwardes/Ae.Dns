using System.Net;

namespace Ae.Dns.Server
{
    public abstract class DnsSocketServerOptions
    {
        public EndPoint Endpoint { get; set; } = new IPEndPoint(IPAddress.Any, 53);
    }
}
