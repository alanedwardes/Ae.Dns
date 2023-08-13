using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        private readonly DnsUdpServerOptions _options;
        private readonly Socket _socket;
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly IDnsRawClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsUdpServer"/> with a custom logger, options and a <see cref="IDnsRawClient"/> to delegate to.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public DnsUdpServer(ILogger<DnsUdpServer> logger, IDnsRawClient dnsClient, IOptions<DnsUdpServerOptions> options)
        {
            _options = options.Value;
            _socket = new Socket(_options.Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(_options.Endpoint);
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <summary>
        /// A convenience constructor where only the <see cref="IDnsRawClient"/> is mandated.
        /// </summary>
        public DnsUdpServer(IDnsRawClient dnsClient, DnsUdpServerOptions options = null)
            : this(NullLogger<DnsUdpServer>.Instance, dnsClient, Options.Create(options ?? new DnsUdpServerOptions()))
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

            _logger.LogInformation("Now listening on: {Endpoint} (DefaultMaximumDatagramSize: {DefaultMaximumDatagramSize})", "udp://" + _options.Endpoint, _options.DefaultMaximumDatagramSize);

            while (!token.IsCancellationRequested)
            {
                var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

                try
                {
                    var result = await _socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, _anyEndpoint, token);
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

        private void TruncateAnswer(Memory<byte> buffer, DnsRawClientResponse response, ref int answerLength)
        {
            var maximumDatagramLength = Math.Max(response.Query.GetMaxUdpMessageSize() ?? 0, _options.DefaultMaximumDatagramSize);
            if (answerLength < maximumDatagramLength)
            {
                // Within acceptable size, do nothing
                return;
            }

            var truncatedAnswer = DnsQueryFactory.TruncateAnswer(response.Query);

            _logger.LogWarning("Truncating answer {Answer} since it is {AnswerLength} bytes (maximum: {MaximumDatagramLength} bytes)", truncatedAnswer, answerLength, maximumDatagramLength);
            
            // Write the truncated answer into the buffer
            answerLength = 0;
            truncatedAnswer.WriteBytes(buffer, ref answerLength);
        }

        private async void Respond(EndPoint sender, Memory<byte> buffer, int queryLength, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            var request = new DnsRawClientRequest(queryLength, sender, nameof(DnsUdpServer));

            DnsRawClientResponse response;
            try
            {
                response = await _dnsClient.Query(buffer, request, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to run query {QueryBytes} for {RemoteEndPoint}", DnsByteExtensions.ToDebugString(buffer.Slice(0, queryLength)), sender);
                return;
            }

            int answerLength = response.AnswerLength;
            try
            {
                // Truncate answer if necessary
                TruncateAnswer(buffer, response, ref answerLength);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to truncate answer {QueryBytes} for {RemoteEndPoint}", DnsByteExtensions.ToDebugString(buffer.Slice(0, answerLength)), sender);
                return;
            }

            try
            {
                // Send the part of the buffer containing the answer
                await _socket.SendToAsync(buffer.Slice(0, answerLength), SocketFlags.None, sender, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to send back answer {AnswerBytes} to {RemoteEndPoint}", DnsByteExtensions.ToDebugString(buffer.Slice(0, answerLength)), sender);
                return;
            }

            _logger.LogInformation("Responded to query from {RemoteEndPoint} in {ResponseTime}", sender, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
