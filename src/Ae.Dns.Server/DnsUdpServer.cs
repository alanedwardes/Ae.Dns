using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
{
    public sealed class DnsUdpServer : IDnsServer
    {
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly UdpClient _listener;
        private readonly IDnsClient _dnsClient;

        public DnsUdpServer(IPEndPoint endpoint, IDnsClient dnsClient)
            : this(new NullLogger<DnsUdpServer>(), endpoint, dnsClient)
        {
        }

        public DnsUdpServer(ILogger<DnsUdpServer> logger, IPEndPoint endpoint, IDnsClient dnsClient)
        {
            _logger = logger;
            _listener = new UdpClient(endpoint);
            _dnsClient = dnsClient;
        }

        public void Dispose()
        {
            try
            {
                _listener.Close();
                _listener.Dispose();
            }
            catch (Exception)
            {
            }
        }

        /// <inheritdoc/>
        public async Task Listen(CancellationToken token)
        {
            token.Register(() => _listener.Close());

            _logger.LogInformation("Server now listening");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener.ReceiveAsync();
                    Respond(result, token);
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing, server shutting down
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error with incoming connection");
                }
            }
        }

        private async void Respond(UdpReceiveResult query, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            DnsHeader message;
            try
            {
                message = DnsByteExtensions.FromBytes<DnsHeader>(query.Buffer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse incoming packet: {0}", DnsByteExtensions.ToDebugString(query.Buffer));
                return;
            }

            DnsAnswer answer;
            try
            {
                answer = await _dnsClient.Query(message, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to resolve {0}", message);
                return;
            }

            var answerBytes = DnsByteExtensions.ToBytes(answer).ToArray();

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
