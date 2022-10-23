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
    public sealed class DnsTcpServer : IDnsServer
    {
        private readonly Socket _socket;
        private readonly ILogger<DnsTcpServer> _logger;
        private readonly IDnsSingleBufferClient _dnsClient;

        public DnsTcpServer(IPEndPoint endpoint, IDnsSingleBufferClient dnsClient)
            : this(new NullLogger<DnsTcpServer>(), endpoint, dnsClient)
        {
        }

        public DnsTcpServer(ILogger<DnsTcpServer> logger, IPEndPoint endpoint, IDnsSingleBufferClient dnsClient)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

            _socket.Listen(1024);

            _logger.LogInformation("Server now listening");

            while (!token.IsCancellationRequested)
            {
#if NETSTANDARD2_1
                var socket = await _socket.AcceptAsync();
#else
                var socket = await _socket.AcceptAsync(token);
#endif
                Connect(socket, token);
            }
        }

        private async void Connect(Socket socket, CancellationToken token)
        {
            using (socket)
            {
#if NETSTANDARD2_1
                ArraySegment<byte> buffer = new byte[65527];
#else
                // Allocate a buffer which will be used for the incoming query, and re-used to send the answer.
                // Also make it pinned, see https://enclave.io/high-performance-udp-sockets-net6/
                Memory<byte> buffer = GC.AllocateArray<byte>(65527, true);
#endif

                while (socket.Connected && !token.IsCancellationRequested)
                {
                    using var idleConnectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var receiveToken = CancellationTokenSource.CreateLinkedTokenSource(token, idleConnectionTimeout.Token);

                    try
                    {
#if NETSTANDARD2_1
                        var queryLength = await socket.ReceiveAsync(buffer, SocketFlags.None, receiveToken.Token);
#else
                        var queryLength = await socket.ReceiveAsync(buffer, SocketFlags.None, receiveToken.Token);
#endif
                        await Respond(socket, buffer, queryLength, token);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Do nothing, connection was closed
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // Do nothing, recieve timed out
                        return;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Error with incoming connection, closing socket");
                        socket.Close();
                    }
                }
            }
        }

#if NETSTANDARD2_1
        private async Task Respond(Socket socket, ArraySegment<byte> buffer, int queryLength, CancellationToken token)
#else
        private async Task Respond(Socket socket, Memory<byte> buffer, int queryLength, CancellationToken token)
#endif
        {
            if (queryLength == 0)
            {
                socket.Close();
            }

            var queryHeaderLength = 0;
#if NETSTANDARD2_1
            var queryLengthCheck = DnsByteExtensions.ReadUInt16(buffer, ref queryHeaderLength);
#else
            var queryLengthCheck = DnsByteExtensions.ReadUInt16(buffer.Span, ref queryHeaderLength);
#endif

            if (queryLength - queryHeaderLength != queryLengthCheck)
            {
                _logger.LogCritical("Recieved query length was not expected", socket.RemoteEndPoint);
            }

            var stopwatch = Stopwatch.StartNew();

            var answerLength = 0;
            try
            {
                answerLength = await _dnsClient.Query(buffer.Slice(queryHeaderLength), queryLength - queryHeaderLength, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to run query for {RemoteEndPoint}", socket.RemoteEndPoint);
                throw;
            }

            var answerHeaderLength = 0;
#if NETSTANDARD2_1
            DnsByteExtensions.ToBytes((ushort)answerLength, buffer, ref answerHeaderLength);
#else
            DnsByteExtensions.ToBytes((ushort)answerLength, buffer.Span, ref answerHeaderLength);
#endif

            try
            {
                // Send the part of the buffer containing the answer
#if NETSTANDARD2_1
                await socket.SendAsync(buffer.Slice(0, answerHeaderLength + answerLength), SocketFlags.None);
#else
                await socket.SendAsync(buffer.Slice(0, answerHeaderLength + answerLength), SocketFlags.None, token);
#endif
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to send back response to {RemoteEndPoint}", socket.RemoteEndPoint);
                throw;
            }

            _logger.LogInformation("Responded to query from {RemoteEndPoint} in {ResponseTime}", socket.RemoteEndPoint, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
