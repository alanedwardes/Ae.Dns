using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
#if NETSTANDARD2_0 || NETSTANDARD2_1
    internal static class SocketTaskExtensions
    {
        public static async Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken token)
        {
            return await socket.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint);
        }

        public static async Task<Socket> AcceptAsync(this Socket socket, CancellationToken token)
        {
            return await socket.AcceptAsync();
        }
#endif

#if NETSTANDARD2_1
        public static async Task<int> SendToAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken token)
        {
            return await socket.SendToAsync(buffer.ToArray(), socketFlags, remoteEP);
        }
#endif

#if NETSTANDARD2_0
        // Implementations from https://github.com/dotnet/corefx/blob/v1.0.0/src/System.Net.Sockets/src/System/Net/Sockets/SocketTaskExtensions.cs
        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            if (!socket.IsBound && socket.SocketType == SocketType.Dgram)
            {
                // Addresses a problem under NETSTANDARD2_0
                // where calling receieve when the socket is not "connected"
                // continually throws an exception (and creates log spam)
                return Task.Delay(TimeSpan.FromMilliseconds(1)).ContinueWith(x => 0);
            }

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
#endif

#if NETSTANDARD2_0 || NETSTANDARD2_1
    }
#endif
}
