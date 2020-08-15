using Ae.DnsResolver.Client;
using Ae.DnsResolver.Repository;
using Ae.DnsResolver.Server;
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

            var filter1 = await DnsSetFilter.CrateFromRemoteHostsFile("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts");
            var filter2 = await DnsSetFilter.CrateFromRemoteHostsFile("https://mirror1.malwaredomains.com/files/justdomains");
            var filter3 = await DnsSetFilter.CrateFromRemoteHostsFile("https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt");

            var repository = new DnsRepository(new[] { cloudFlare, google1, google2 }, new MemoryCache("dns"), new[] { filter1, filter2, filter3 });

            var server = new DnsUdpServer(new UdpClient(53), repository);

            await server.Recieve(CancellationToken.None);
        }
    }
}
