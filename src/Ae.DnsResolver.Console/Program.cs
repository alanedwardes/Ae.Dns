using Ae.DnsResolver.Client;
using System;
using System.Linq;
using System.Net;
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

            await Task.WhenAll(ListenTcp(client), ListenUdp(client));
        }

        private static async Task ListenTcp(DnsUdpClient client)
        {
            var tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 53);

            tcpListener.Start();

            while (true)
            {
                var accept = await tcpListener.AcceptSocketAsync();
            }
        }

        private static async Task ListenUdp(DnsUdpClient client)
        {
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

            byte[] result;
            try
            {
                result = await client.LookupRaw(r.Buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            var offset = 0;
            var response = DnsMessageReader.ReadDnsResponse(result, ref offset);

            Console.WriteLine(response);

            await listener.SendAsync(result, result.Length, r.RemoteEndPoint);
        }
    }
}
