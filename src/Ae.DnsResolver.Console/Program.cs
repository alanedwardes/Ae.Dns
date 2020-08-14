using Ae.DnsResolver.Client;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Ae.DnsResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            DoWork().GetAwaiter().GetResult();
            Console.WriteLine("Hello World!");
        }

        private static async Task DoWork()
        {
            var client = new DnsUdpClient(new UdpClient("1.1.1.1", 53));

            var listener = new UdpClient(53);

            while (true)
            {
                try
                {
                    var result = await listener.ReceiveAsync();
                    Respond(result, client, listener);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Connection forcibly closed");
                    continue;
                }
            }
        }

        public static async void Respond(UdpReceiveResult r, DnsUdpClient client, UdpClient listener)
        {
            var message = DnsMessageReader.ReadDnsMessage(r.Buffer);

            var test = string.Join(", ", r.Buffer.Select(x => x));

            Console.WriteLine(message);

            var result = await client.LookupRaw(r.Buffer);

            var offset = 0;
            var response = DnsMessageReader.ReadDnsResponse(result, ref offset);

            Console.WriteLine(response);

            await listener.SendAsync(result, result.Length, r.RemoteEndPoint);
        }
    }
}
