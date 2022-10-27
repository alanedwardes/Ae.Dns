using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// An experimental DNS TCP client.
    /// </summary>
    [Obsolete("This class is experimental.")]
    public sealed class DnsTcpClient : IDnsClient
    {
        private readonly ILogger<DnsTcpClient> _logger;
        private readonly IPEndPoint _endpoint;
        private bool _isDisposed;

        public DnsTcpClient(IPAddress address) :
            this(new NullLogger<DnsTcpClient>(), address)
        {
        }

        public DnsTcpClient(IPEndPoint endpoint) :
            this(new NullLogger<DnsTcpClient>(), endpoint)
        {
        }

        public DnsTcpClient(ILogger<DnsTcpClient> logger, IPAddress address) :
            this(logger, new IPEndPoint(address, 53))
        {
        }

        public DnsTcpClient(ILogger<DnsTcpClient> logger, IPEndPoint endpoint)
        {
            _logger = logger;
            _endpoint = endpoint;
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

            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(_endpoint);

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
        public override string ToString() => $"tcp://{_endpoint}/";
    }
}
