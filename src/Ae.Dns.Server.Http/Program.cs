using Ae.Dns.Client;
using System.Net;
using System.Threading;

namespace Ae.Dns.Server.Http
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

            var server = new DnsHttpServer(dnsClient, x => x);

            server.Listen(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
