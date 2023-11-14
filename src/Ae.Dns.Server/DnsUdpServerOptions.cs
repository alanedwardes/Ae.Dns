namespace Ae.Dns.Server
{
    /// <summary>
    /// Defines options for the <see cref="DnsUdpServer"/>.
    /// </summary>
    public sealed class DnsUdpServerOptions : DnsSocketServerOptions
    {
        /// <summary>
        /// The maximum datagram size before truncation.
        /// </summary>
        public uint DefaultMaximumDatagramSize { get; set; } = 512;
    }
}
