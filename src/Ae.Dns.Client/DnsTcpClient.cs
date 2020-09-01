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
    [Obsolete("This class is experimental.")]
    public sealed class DnsTcpClient : IDnsClient, IDisposable
    {
        private readonly ILogger<DnsTcpClient> _logger;
        private readonly Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        private readonly string _label;

        public DnsTcpClient(ILogger<DnsTcpClient> logger, IPAddress address, string label)
        {
            _logger = logger;
            _socket.Connect(address, 53);
            _label = label;
        }

        public void Dispose()
        {
            try
            {
                _socket.Disconnect(false);
            }
            catch (Exception)
            {
            }
        }

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var raw = query.ToBytes().ToArray();

            var payload = ((ushort)raw.Length).ToBytes().Concat(raw).ToArray();
            await _socket.SendAsync(payload, SocketFlags.None, token);

            var buffer = new byte[4096];
            var recieve = await _socket.ReceiveAsync(buffer, SocketFlags.None, token);

            var offset = 0;
            var responseLength = buffer.ReadUInt16(ref offset);

            var response = buffer.ReadBytes(responseLength, ref offset);

            if (response.Length != responseLength)
            {
                throw new InvalidOperationException();
            }

            return response.FromBytes<DnsAnswer>();
        }
    }
}
