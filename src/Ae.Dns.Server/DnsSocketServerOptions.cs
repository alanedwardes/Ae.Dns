using System.Net;

namespace Ae.Dns.Server
{
    /// <summary>
    /// Defines options common to all DNS socket servers.
    /// </summary>
    public abstract class DnsSocketServerOptions
    {
        /// <summary>
        /// The default endpoint to listen on, for example 0.0.0.0:53
        /// </summary>
        public EndPoint Endpoint { get; set; } = new IPEndPoint(IPAddress.Any, 53);
    }
}
