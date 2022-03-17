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
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Ae.Dns.Server
{
    public sealed class DnsUdpServer : IDnsServer
    {
        private static readonly Meter _meter = new Meter("Ae.Dns.Server.DnsUdpServer");
        private static readonly Counter<int> _incomingErrorCounter = _meter.CreateCounter<int>("IncomingReadError");
        private static readonly Counter<int> _packetParseErrorCounter = _meter.CreateCounter<int>("RequestParseError");
        private static readonly Counter<int> _lookupErrorCounter = _meter.CreateCounter<int>("LookupError");
        private static readonly Counter<int> _respondErrorCounter = _meter.CreateCounter<int>("RespondError");
        private static readonly Counter<int> _responseCounter = _meter.CreateCounter<int>("Response");
        private static readonly Counter<int> _requestCounter = _meter.CreateCounter<int>("Request");
        private static readonly Counter<int> _queryCounter = _meter.CreateCounter<int>("Query");

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
                    _requestCounter.Add(1);
                    Respond(result, token);
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing, server shutting down
                    return;
                }
                catch (Exception e)
                {
                    _incomingErrorCounter.Add(1);
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
                _packetParseErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to parse incoming packet: {0}", DnsByteExtensions.ToDebugString(query.Buffer));
                return;
            }

            var queryMetricState = new KeyValuePair<string, object>("Query", message);
            _queryCounter.Add(1, queryMetricState);

            DnsAnswer answer;
            try
            {
                answer = await _dnsClient.Query(message, token);
            }
            catch (Exception e)
            {
                _lookupErrorCounter.Add(1);
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
                _respondErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to send back response to {0}", query.RemoteEndPoint);
                return;
            }

            _responseCounter.Add(1);
            _logger.LogTrace("Responded to DNS request for {Domain} in {ResponseTime}", message.Host, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
