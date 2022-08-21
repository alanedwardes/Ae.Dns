using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
            var raw = DnsByteExtensions.ToBytes(query).ToArray();

            var payload = DnsByteExtensions.ToBytes((ushort)raw.Length).Concat(raw).ToArray();
            var stream = _socket.GetStream();

            await stream.WriteAsync(payload, 0, payload.Length, token);

            var buffer = new byte[4096];
            var receive = await stream.ReadAsync(buffer, 0, buffer.Length, token);

            stream.Close();

            var offset = 0;
            var responseLength = DnsByteExtensions.ReadUInt16(buffer, ref offset);

            var response = DnsByteExtensions.ReadBytes(buffer, responseLength, ref offset);

            if (response.Length != responseLength)
            {
                throw new InvalidOperationException();
            }

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(response);
            answer.Header.Tags.Add("Resolver", this);
            return answer;
        }

        /// <inheritdoc/>
        public override string ToString() => $"tcp://{_socket.Client.RemoteEndPoint}/";
    }
}
