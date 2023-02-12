using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
{
    public sealed class DnsTcpServer : IDnsServer
    {
        private readonly Socket _socket;
        private readonly ILogger<DnsTcpServer> _logger;
        private readonly DnsTcpServerOptions _options;
        private readonly IDnsRawClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsTcpServer"/> with a custom logger, options and a <see cref="IDnsRawClient"/> to delegate to.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public DnsTcpServer(ILogger<DnsTcpServer> logger, IOptions<DnsTcpServerOptions> options, IDnsRawClient dnsClient)
        {
            _options = options.Value;
            _socket = new Socket(_options.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_options.Endpoint);
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <summary>
        /// A convenience constructor where only the <see cref="IDnsRawClient"/> is mandated.
        /// </summary>
        public DnsTcpServer(IDnsRawClient dnsClient, DnsTcpServerOptions options = null) : this(NullLogger<DnsTcpServer>.Instance, Options.Create(options ?? new DnsTcpServerOptions()), dnsClient)
        {
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

            _socket.Listen(_options.Backlog);

            _logger.LogInformation("Now listening on: {Endpoint} (Backlog: {Backlog})", "tcp://" + _options.Endpoint, _options.Backlog);

            while (!token.IsCancellationRequested)
            {
#if NETSTANDARD2_0 || NETSTANDARD2_1
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
                var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

                while (socket.Connected && !token.IsCancellationRequested)
                {
                    using var idleConnectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var receiveToken = CancellationTokenSource.CreateLinkedTokenSource(token, idleConnectionTimeout.Token);

                    try
                    {
                        var queryLength = await Receive(socket, buffer, token);
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

        private async Task<int> Receive(Socket socket, Memory<byte> buffer, CancellationToken token)
        {
            async Task<int> DoReceive(Memory<byte> bufferPart)
            {
                var receivedBytes = await socket.ReceiveAsync(bufferPart, SocketFlags.None, token);
                if (receivedBytes == 0)
                {
                    socket.Close();
                }

                return receivedBytes;
            }

            // Perform an initial receive to obtain the first
            // chunk and query length, but may need more data
            var bufferOffset = await DoReceive(buffer);

            var queryHeaderLength = 0;
            var queryLength = DnsByteExtensions.ReadUInt16(buffer, ref queryHeaderLength);

            // Keep receiving until we have enough data
            while (socket.Connected && bufferOffset - queryHeaderLength < queryLength)
            {
                bufferOffset += await DoReceive(buffer.Slice(bufferOffset));
            }

            return queryLength;
        }

        private async Task Respond(Socket socket, Memory<byte> buffer, int queryLength, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            DnsRawClientResponse response;
            try
            {
                response = await _dnsClient.Query(buffer.Slice(2), queryLength, socket.RemoteEndPoint, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to run query for {RemoteEndPoint}", socket.RemoteEndPoint);
                throw;
            }

            var answerLength = response.AnswerLength;

            var answerHeaderLength = 0;
            DnsByteExtensions.ToBytes((ushort)answerLength, buffer, ref answerHeaderLength);

            try
            {
                // Slice to the part of the buffer containing the answer and send it
                var answerBuffer = buffer.Slice(0, answerHeaderLength + answerLength);
                await socket.SendAsync(answerBuffer, SocketFlags.None, token);
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
