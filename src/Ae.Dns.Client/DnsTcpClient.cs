using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// An experimental DNS TCP client.
    /// </summary>
    public sealed class DnsTcpClient : IDnsClient
    {
        private readonly ILogger<DnsTcpClient> _logger;
        private readonly DnsTcpClientOptions _options;
        private bool _isDisposed;

        [ActivatorUtilitiesConstructor]
        public DnsTcpClient(ILogger<DnsTcpClient> logger, IOptions<DnsTcpClientOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public DnsTcpClient(DnsTcpClientOptions options) : this(NullLogger<DnsTcpClient>.Instance, Options.Create(options))
        {
        }

        /// <inheritdoc/>
        public void Dispose() => _isDisposed = true;

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using var socket = new Socket(_options.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(_options.Endpoint);

            var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

            var sendOffset = sizeof(ushort);
            query.WriteBytes(buffer, ref sendOffset);

            var fakeOffset = 0;
            DnsByteExtensions.ToBytes((ushort)(sendOffset - sizeof(ushort)), buffer, ref fakeOffset);

            var sendBuffer = buffer.Slice(0, sendOffset);

            await socket.SendAsync(sendBuffer, SocketFlags.None, token);

            var received = await socket.ReceiveAsync(buffer, SocketFlags.None, token);

            var offset = 0;
            var answerLength = DnsByteExtensions.ReadUInt16(buffer, ref offset);

            while (received < answerLength)
            {
                received += await socket.ReceiveAsync(buffer.Slice(received), SocketFlags.None, token);
            }

            socket.Close();

            var answerBuffer = buffer.Slice(offset, answerLength);

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(answerBuffer);
            answer.Header.Tags.Add("Resolver", ToString());
            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => $"tcp://{_options.Endpoint}/";
    }
}
