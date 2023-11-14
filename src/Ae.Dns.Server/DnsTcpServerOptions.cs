namespace Ae.Dns.Server
{
    /// <summary>
    /// Defines options for the <see cref="DnsTcpServer"/>.
    /// </summary>
    public sealed class DnsTcpServerOptions : DnsSocketServerOptions
    {
        /// <summary>
        /// The socket backlog.
        /// </summary>
        public int Backlog { get; set; } = 1024;
    }
}
