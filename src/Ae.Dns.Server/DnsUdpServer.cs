using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
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

        private static readonly EndPoint _anyEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly Socket _socket;
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly IDnsClient _dnsClient;

        public DnsUdpServer(IPEndPoint endpoint, IDnsClient dnsClient)
            : this(new NullLogger<DnsUdpServer>(), endpoint, dnsClient)
        {
        }

        public DnsUdpServer(ILogger<DnsUdpServer> logger, IPEndPoint endpoint, IDnsClient dnsClient)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(endpoint);
            _logger = logger;
            _dnsClient = dnsClient;
        }

        public void Dispose()
        {
            try
            {
                _socket.Close();
                _socket.Dispose();
            }
            catch (Exception)
            {
            }
        }

        /// <inheritdoc/>
        public async Task Listen(CancellationToken token)
        {
            token.Register(() => _socket.Close());

            _logger.LogInformation("Server now listening");

            while (!token.IsCancellationRequested)
            {
                // Allocate a buffer which will be used for the incoming query, and re-used to send the answer.
                // Also make it pinned, see https://enclave.io/high-performance-udp-sockets-net6/
                Memory<byte> buffer = GC.AllocateArray<byte>(65527, true);

                try
                {
                    var result = await _socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, _anyEndpoint, token);
                    _requestCounter.Add(1);
                    Respond(result.RemoteEndPoint, buffer, result.ReceivedBytes, token);
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

        private async void Respond(EndPoint sender, Memory<byte> buffer, int queryLength, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var stopwatchMetricState = new KeyValuePair<string, object>("Stopwatch", stopwatch);

            DnsMessage query;
            try
            {
                query = DnsByteExtensions.FromBytes<DnsMessage>(buffer.Span.Slice(0, queryLength));
            }
            catch (Exception e)
            {
                _packetParseErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to parse incoming packet {PacketBytes}", DnsByteExtensions.ToDebugString(buffer.Slice(0, queryLength).ToArray()));
                return;
            }

            var queryMetricState = new KeyValuePair<string, object>("Query", query);
            _queryCounter.Add(1, queryMetricState);

            DnsMessage answer;
            try
            {
                answer = await _dnsClient.Query(query, token);
            }
            catch (Exception e)
            {
                _lookupErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to resolve {Query}", query);
                return;
            }

            var answerMetricState = new KeyValuePair<string, object>("Answer", answer);

            var answerLength = 0;
            try
            {
                // Write data to our existing buffer (write over the query)
                answer.WriteBytes(buffer.Span, ref answerLength);
            }
            catch (Exception e)
            {
                _respondErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to serialise {Answer}", answer);
                return;
            }

            try
            {
                // Send the part of the buffer containing the answer
                await _socket.SendToAsync(buffer.Slice(0, answerLength), SocketFlags.None, sender, token);
            }
            catch (Exception e)
            {
                _respondErrorCounter.Add(1);
                _logger.LogCritical(e, "Unable to send back response to {RemoteEndPoint}", sender);
                return;
            }
            finally
            {
                stopwatch.Stop();
            }

            _responseCounter.Add(1, queryMetricState, answerMetricState, stopwatchMetricState);
            _logger.LogInformation("Responded to {Query} from {RemoteEndPoint} in {ResponseTime}", answer, sender, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
