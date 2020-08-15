using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Server
{
    public sealed class DnsUdpServer
    {
        private readonly UdpClient _listner;
        private readonly IDnsClient _dnsClient;

        public DnsUdpServer(UdpClient listner, IDnsClient dnsClient)
        {
            _listner = listner;
            _dnsClient = dnsClient;
        }

        public async Task Recieve(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Respond(await _listner.ReceiveAsync());
                }
                catch (Exception e)
                {
                    // Do nothing
                }
            }
        }

        private async void Respond(UdpReceiveResult query)
        {
            var message = DnsMessageReader.ReadDnsMessage(query.Buffer);

            Console.WriteLine(message);

            byte[] answer;
            try
            {
                answer = await _dnsClient.LookupRaw(query.Buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            var offset = 0;
            var response = DnsMessageReader.ReadDnsResponse(answer, ref offset);

            Console.WriteLine(response);

            await _listner.SendAsync(answer, answer.Length, query.RemoteEndPoint);
        }
    }
}
