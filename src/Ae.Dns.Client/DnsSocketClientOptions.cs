using System;
using System.Net;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Describes options common to all socket-based clients.
    /// </summary>
    public abstract class DnsSocketClientOptions
    {
        /// <summary>
        /// The time before a DNS query is considered failed.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>
        /// The endpoint to connect to.
        /// </summary>
        public EndPoint Endpoint { get; set; }
    }

    /// <summary>
    /// Defines options for the <see cref="DnsUdpClient"/>.
    /// </summary>
    public sealed class DnsUdpClientOptions : DnsSocketClientOptions
    {
        /// <summary>
        /// Convert an <see cref="IPAddress"/> to a <see cref="DnsUdpClientOptions"/>.
        /// </summary>
        /// <param name="d"></param>
        public static implicit operator DnsUdpClientOptions(IPAddress d) => new DnsUdpClientOptions { Endpoint = new IPEndPoint(d, 53) };
        /// <summary>
        /// Convert an <see cref="IPEndPoint"/> to a <see cref="DnsUdpClientOptions"/>.
        /// </summary>
        /// <param name="d"></param>
        public static implicit operator DnsUdpClientOptions(IPEndPoint d) => new DnsUdpClientOptions { Endpoint = d };
    }

    /// <summary>
    /// Defines options for the <see cref="DnsTcpClient"/>.
    /// </summary>
    public sealed class DnsTcpClientOptions : DnsSocketClientOptions
    {
        /// <summary>
        /// Convert an <see cref="IPAddress"/> to a <see cref="DnsTcpClientOptions"/>.
        /// </summary>
        /// <param name="d"></param>
        public static implicit operator DnsTcpClientOptions(IPAddress d) => new DnsTcpClientOptions { Endpoint = new IPEndPoint(d, 53) };
        /// <summary>
        /// Convert an <see cref="IPEndPoint"/> to a <see cref="DnsTcpClientOptions"/>.
        /// </summary>
        /// <param name="d"></param>
        public static implicit operator DnsTcpClientOptions(IPEndPoint d) => new DnsTcpClientOptions { Endpoint = d };
    }
}
