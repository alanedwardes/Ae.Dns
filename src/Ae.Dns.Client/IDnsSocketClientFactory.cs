using System.Net;

namespace Ae.Dns.Client
{
    public interface IDnsSocketClientFactory
    {
        IDnsClient CreateTcpClient(IPAddress address);
        IDnsClient CreateUdpClient(IPAddress address);
    }
}