using System.Net;

namespace Ae.Dns.Client
{
    public abstract class DnsSocketClientOptions
    {
        public EndPoint Endpoint { get; set; }
    }

    public sealed class DnsUdpClientOptions : DnsSocketClientOptions
    {
        public static implicit operator DnsUdpClientOptions(IPAddress d) => new DnsUdpClientOptions { Endpoint = new IPEndPoint(d, 53) };
        public static implicit operator DnsUdpClientOptions(IPEndPoint d) => new DnsUdpClientOptions { Endpoint = d };
    }

    public sealed class DnsTcpClientOptions : DnsSocketClientOptions
    {
        public static implicit operator DnsTcpClientOptions(IPAddress d) => new DnsTcpClientOptions { Endpoint = new IPEndPoint(d, 53) };
        public static implicit operator DnsTcpClientOptions(IPEndPoint d) => new DnsTcpClientOptions { Endpoint = d };
    }
}
