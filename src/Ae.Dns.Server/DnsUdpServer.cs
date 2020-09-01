using Ae.Dns.Protocol;
using Ae.Dns.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
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
            var stopwatch = Stopwatch.StartNew();

            DnsHeader message;
            try
            {
                message = query.Buffer.ReadDnsHeader();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse incoming packet: {0}", query.Buffer.ToDebugString());
                return;
            }

            DnsAnswer answer;
            try
            {
                var header = query.Buffer.ReadDnsHeader();
                answer = await _dnsRepository.Resolve(header);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to resolve {0}", message);
                return;
            }

            var answerBytes = answer.ToBytes().ToArray();

            try
            {
                await _listener.SendAsync(answerBytes, answerBytes.Length, query.RemoteEndPoint);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to send back response to {0}", query.RemoteEndPoint);
                return;
            }

            _logger.LogTrace("Responded to DNS request for {Domain} in {ResponseTime}", message.Host, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
