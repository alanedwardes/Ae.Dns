using Ae.Dns.Protocol;
using System.Net;

namespace Ae.Dns.Server
{
    public sealed class DnsUdpServerOptions : DnsSocketServerOptions
    {
        public uint DefaultMaximumDatagramSize { get; set; } = 512;
    }
}
