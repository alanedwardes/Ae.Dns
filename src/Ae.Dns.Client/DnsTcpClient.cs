using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
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
        private readonly IPAddress _address;
        private readonly TcpClient _socket;

        public DnsTcpClient(ILogger<DnsTcpClient> logger, IPAddress address)
        {
            _logger = logger;
            _address = address;
            _socket = new TcpClient(address.AddressFamily);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _socket.Close();
            _socket.Dispose();
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            if (!_socket.Connected)
            {
                await _socket.ConnectAsync(_address, 53);
            }

            var stream = _socket.GetStream();

            var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

            var sendOffset = sizeof(ushort);
#if NETSTANDARD2_1
            query.WriteBytes(buffer, ref sendOffset);
#else
            query.WriteBytes(buffer.Span, ref sendOffset);
#endif

            var fakeOffset = 0;
#if NETSTANDARD2_1
            DnsByteExtensions.ToBytes((ushort)(sendOffset - sizeof(ushort)), buffer, ref fakeOffset);
#else
            DnsByteExtensions.ToBytes((ushort)(sendOffset - sizeof(ushort)), buffer.Span, ref fakeOffset);
#endif

            var sendBuffer = buffer.Slice(0, sendOffset);

            await stream.WriteAsync(sendBuffer, token);

            var received = await stream.ReadAsync(buffer, token);

            var offset = 0;
#if NETSTANDARD2_1
            var answerLength = DnsByteExtensions.ReadUInt16(buffer, ref offset);
#else
            var answerLength = DnsByteExtensions.ReadUInt16(buffer.Span, ref offset);
#endif

            while (received < answerLength)
            {
                received += await stream.ReadAsync(buffer.Slice(received), token);
            }

            var answerBuffer = buffer.Slice(offset, answerLength);

#if NETSTANDARD2_1
            var answer = DnsByteExtensions.FromBytes<DnsMessage>(answerBuffer);
#else
            var answer = DnsByteExtensions.FromBytes<DnsMessage>(answerBuffer.Span);
#endif
            answer.Header.Tags.Add("Resolver", ToString());
            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => $"tcp://{_socket.Client.RemoteEndPoint}/";
    }
}
