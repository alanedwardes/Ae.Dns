using Ae.DnsResolver.Client;
using Ae.DnsResolver.Server;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            DoWork().GetAwaiter().GetResult();
        }

        private static async Task DoWork()
        {
            var client = new DnsUdpClient(new UdpClient("1.1.1.1", 53));

            var server = new DnsUdpServer(new UdpClient(53), client);

            await server.Recieve(CancellationToken.None);
        }
    }
}
