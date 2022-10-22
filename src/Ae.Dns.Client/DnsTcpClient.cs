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
        private readonly TcpClient _socket;

        public DnsTcpClient(ILogger<DnsTcpClient> logger, IPAddress address)
        {
            _logger = logger;
            _socket = new TcpClient(address.AddressFamily);
            _socket.Connect(address, 53);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _socket.Close();
            _socket.Dispose();
        }

        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var sendBuffer = new byte[65527];

            var sendOffset = sizeof(ushort);
            query.WriteBytes(sendBuffer, ref sendOffset);

            var fakeOffset = 0;
            DnsByteExtensions.ToBytes((ushort)(sendOffset - sizeof(ushort)), sendBuffer, ref fakeOffset);

            var stream = _socket.GetStream();

            await stream.WriteAsync(sendBuffer, 0, sendOffset, token);

            var buffer = new byte[4096];
            var receive = await stream.ReadAsync(buffer, 0, buffer.Length, token);

            var offset = 0;
            var responseLength = DnsByteExtensions.ReadUInt16(buffer, ref offset);

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(DnsByteExtensions.ReadBytes(buffer, responseLength, ref offset));
            answer.Header.Tags.Add("Resolver", ToString());
            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => $"tcp://{_socket.Client.RemoteEndPoint}/";
    }
}
