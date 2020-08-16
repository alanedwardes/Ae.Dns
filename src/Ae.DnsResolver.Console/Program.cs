using Ae.DnsResolver.Client;
using Ae.DnsResolver.Repository;
using Ae.DnsResolver.Server;
using System;
using System.Net.Sockets;
using System.Runtime.Caching;
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
            var cloudFlare = new DnsUdpClient(new UdpClient("1.1.1.1", 53));
            var google1 = new DnsUdpClient(new UdpClient("8.8.8.8", 53));
            var google2 = new DnsUdpClient(new UdpClient("8.8.4.4", 53));

            var filter = new DnsRemoteSetFilter();

            await Task.WhenAll(
                filter.AddRemoteList(new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts")),
                filter.AddRemoteList(new Uri("https://mirror1.malwaredomains.com/files/justdomains")),
                filter.AddRemoteList(new Uri("https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt"))
            );

            var combinedDnsClient = new DnsCompositeClient(cloudFlare, google1, google2);

            var repository = new DnsRepository(combinedDnsClient, new MemoryCache("dns"), filter);

            var server = new DnsUdpServer(new UdpClient(53), repository);

            await server.Recieve(CancellationToken.None);
        }
    }
}
