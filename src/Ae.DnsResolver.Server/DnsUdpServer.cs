using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Repository;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Server
{
    public sealed class DnsUdpServer
    {
        private readonly UdpClient _listner;
        private readonly IDnsRepository _dnsRepository;

        public DnsUdpServer(UdpClient listner, IDnsRepository dnsRepository)
        {
            _listner = listner;
            _dnsRepository = dnsRepository;
        }

        public async Task Recieve(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listner.ReceiveAsync();
                    Respond(result);
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
                answer = await _dnsRepository.Resolve(query.Buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            await _listner.SendAsync(answer, answer.Length, query.RemoteEndPoint);
        }
    }
}
