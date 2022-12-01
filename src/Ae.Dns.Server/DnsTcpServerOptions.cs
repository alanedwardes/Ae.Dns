namespace Ae.Dns.Server
{
    public sealed class DnsTcpServerOptions : DnsSocketServerOptions
    {
        public int Backlog { get; set; } = 1024;
    }
}
