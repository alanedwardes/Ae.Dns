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
            var client = new DnsUdpClient(new UdpClient("192.168.1.42", 53));

            var filter1 = await DnsSetFilter.CrateFromRemoteHostsFile("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts");
            var filter2 = await DnsSetFilter.CrateFromRemoteHostsFile("https://mirror1.malwaredomains.com/files/justdomains");

            var repository = new DnsRepository(client, new MemoryCache("dns"), new[] { filter1, filter2 });

            var server = new DnsUdpServer(new UdpClient(53), repository);

            await server.Recieve(CancellationToken.None);
        }
    }
}
