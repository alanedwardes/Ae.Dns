using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    internal static class SocketTaskExtensions

    {
#if NETSTANDARD2_1
        public static async Task<int> SendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken token)
        {
            return await socket.SendToAsync(buffer.ToArray(), socketFlags, remoteEP);
        }
#endif

#if NETSTANDARD2_0
        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) => ((Socket)state).BeginReceive(
                                                              targetBuffer.Array,
                                                              targetBuffer.Offset,
                                                              targetBuffer.Count,
                                                              flags,
                                                              callback,
                                                              state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndReceive(asyncResult),
                buffer,
                socketFlags,
                state: socket);
        }

        public static async Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, CancellationToken token)
        {
            return await socket.ReceiveAsync(buffer, socketFlags);
        }

        public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public static async Task<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken token)
        {
            return await socket.SendAsync(new ArraySegment<byte>(buffer.ToArray()), socketFlags);
        }

        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) => ((Socket)state).BeginSend(
                                                              targetBuffer.Array,
                                                              targetBuffer.Offset,
                                                              targetBuffer.Count,
                                                              flags,
                                                              callback,
                                                              state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSend(asyncResult),
                buffer,
                socketFlags,
                state: socket);
        }

        public static Task<int> SendToAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, endPoint, callback, state) => ((Socket)state).BeginSendTo(
                                                                        targetBuffer.Array,
                                                                        targetBuffer.Offset,
                                                                        targetBuffer.Count,
                                                                        flags,
                                                                        endPoint,
                                                                        callback,
                                                                        state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSendTo(asyncResult),
                buffer,
                socketFlags,
                remoteEndPoint,
                state: socket);
        }

        public static async Task<int> SendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken token)
        {
            return await socket.SendToAsync(new ArraySegment<byte>(buffer.ToArray()), socketFlags, remoteEP);
        }

        public static ArraySegment<byte> Slice(this ArraySegment<byte> buffer, int start, int length)
        {
            return new ArraySegment<byte>(buffer.Array, start, length);
        }

        public static ArraySegment<byte> Slice(this ArraySegment<byte> buffer, int length)
        {
            return Slice(buffer, 0, length);
        }
#endif
    }
}
