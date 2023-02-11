using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    internal static class DnsSocketExtensions
    {
#if NETSTANDARD2_0
        public static Task<int> DnsSendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            var arraySegment = new ArraySegment<byte>(buffer.Span.ToArray());
            socket.BeginSendTo(arraySegment.Array, arraySegment.Offset, arraySegment.Count, SocketFlags.None, remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSendTo(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }
#elif NETSTANDARD2_1
        public static async Task<int> DnsSendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken token)
        {
            return await socket.SendToAsync(buffer.ToArray(), SocketFlags.None, remoteEP);
        }
#else
        public static async Task<int> DnsSendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken token)
        {
            return await socket.SendToAsync(buffer, SocketFlags.None, remoteEP, token);
        }
#endif

#if NETSTANDARD2_0
        public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            var arraySegment = new ArraySegment<byte>(buffer.Span.ToArray());
            socket.BeginReceive(arraySegment.Array, arraySegment.Offset, arraySegment.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            var arraySegment = new ArraySegment<byte>(buffer.Span.ToArray());
            socket.BeginSend(arraySegment.Array, arraySegment.Offset, arraySegment.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }
#endif
    }
}
