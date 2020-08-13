using System;
using System.Collections.Concurrent;
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

        public struct RecordType
        {
            public string Name;
            public Qtype Type;
            public Qclass Class;
        }

        public static RecordType ToRecordType(DnsMessage message)
        {
            return new RecordType
            {
                Name = string.Join(".", message.Labels),
                Type = message.Qtype,
                Class = message.Qclass
            };
        }

        private static ConcurrentDictionary<RecordType, TaskCompletionSource<byte[]>> _cache = new ConcurrentDictionary<RecordType, TaskCompletionSource<byte[]>>();

        private static async Task DoWork()
        {
            var client = new UdpClient("1.1.1.1", 53);

            var listener = new UdpClient(53);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var result = await client.ReceiveAsync();

                    var test = string.Join(",", result.Buffer.Select(x => x));

                    //var message = DnsMessageReader.ReadDnsResponse(result.Buffer);

                    //Console.WriteLine(message);

                    //if (_cache.TryGetValue(ToRecordType(message), out var completionSource))
                    //{
                    //    completionSource.SetResult(result.Buffer);
                    //}
                }
            });

            while (true)
            {
                UdpReceiveResult r;
                try
                {
                    r = await listener.ReceiveAsync();
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Connection forcibly closed");
                    continue;
                }

                Respond(r, client, listener);
            }
        }

        public static async void Respond(UdpReceiveResult r, UdpClient client, UdpClient listener)
        {
            var message = DnsMessageReader.ReadDnsMessage(r.Buffer);

            Console.WriteLine(message);

            var completionSource = _cache.GetOrAdd(ToRecordType(message), key =>
            {
                client.SendAsync(r.Buffer, r.Buffer.Length);
                return new TaskCompletionSource<byte[]>();
            });

            var result = await completionSource.Task;

            await listener.SendAsync(result, result.Length, r.RemoteEndPoint);
        }
    }
}
