using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    internal static class DnsSocketExtensions
    {
        public static async Task<int> DnsSendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken token)
        {
#if NETSTANDARD2_1
            return await socket.SendToAsync(buffer.ToArray(), SocketFlags.None, remoteEP);
#else
            return await socket.SendToAsync(buffer, SocketFlags.None, remoteEP, token);
#endif
        }
    }
}
