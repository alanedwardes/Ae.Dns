using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Server
{
    public sealed class DnsUdpServer
    {
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly UdpClient _listener;
        private readonly IDnsRepository _dnsRepository;

        public DnsUdpServer(ILogger<DnsUdpServer> logger, UdpClient listener, IDnsRepository dnsRepository)
        {
            _logger = logger;
            _listener = listener;
            _dnsRepository = dnsRepository;
        }

        public async Task Recieve(CancellationToken token)
        {
            _logger.LogInformation("Server now listening");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener.ReceiveAsync();
                    Respond(result);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error with incoming connection");
                }
            }
        }

        private async void Respond(UdpReceiveResult query)
        {
            var offset = 0;
            var message = query.Buffer.ReadDnsHeader(ref offset);

            _logger.LogTrace("Request: {0}", message);

            byte[] answer;
            try
            {
                answer = await _dnsRepository.Resolve(query.Buffer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to resolve {0}", message);
                return;
            }

            await _listener.SendAsync(answer, answer.Length, query.RemoteEndPoint);
        }
    }
}
