using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
{
    public sealed class DnsUdpServer : IDnsServer
    {
        private static readonly EndPoint _anyEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly Socket _socket;
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly IDnsSingleBufferClient _dnsClient;

        public DnsUdpServer(IPEndPoint endpoint, IDnsSingleBufferClient dnsClient)
            : this(new NullLogger<DnsUdpServer>(), endpoint, dnsClient)
        {
        }

        public DnsUdpServer(ILogger<DnsUdpServer> logger, IPEndPoint endpoint, IDnsSingleBufferClient dnsClient)
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
#if NETSTANDARD2_1
                ArraySegment<byte> buffer = new byte[65527];
#else
                // Allocate a buffer which will be used for the incoming query, and re-used to send the answer.
                // Also make it pinned, see https://enclave.io/high-performance-udp-sockets-net6/
                Memory<byte> buffer = GC.AllocateArray<byte>(65527, true);
#endif

                try
                {
#if NETSTANDARD2_1
                    var result = await _socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, _anyEndpoint);
#else
                    var result = await _socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, _anyEndpoint, token);
#endif
                    Respond(result.RemoteEndPoint, buffer, result.ReceivedBytes, token);
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

#if NETSTANDARD2_1
        private async void Respond(EndPoint sender, ArraySegment<byte> buffer, int queryLength, CancellationToken token)
#else
        private async void Respond(EndPoint sender, Memory<byte> buffer, int queryLength, CancellationToken token)
#endif
        {
            var stopwatch = Stopwatch.StartNew();

            var answerLength = 0;
            try
            {
                answerLength = await _dnsClient.Query(buffer, queryLength, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to run query for {RemoteEndPoint}", sender);
            }

            try
            {
                // Send the part of the buffer containing the answer
#if NETSTANDARD2_1
                await _socket.SendToAsync(buffer.Slice(0, answerLength), SocketFlags.None, sender);
#else
                await _socket.SendToAsync(buffer.Slice(0, answerLength), SocketFlags.None, sender, token);
#endif
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to send back response to {RemoteEndPoint}", sender);
                return;
            }

            _logger.LogInformation("Responded to query from {RemoteEndPoint} in {ResponseTime}", sender, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
