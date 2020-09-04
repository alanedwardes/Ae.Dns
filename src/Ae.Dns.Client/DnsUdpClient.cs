using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public sealed class DnsUdpClient : IDisposable, IDnsClient
    {
        private struct MessageId
        {
            public ushort Id;
            public string Name;
            public DnsQueryType Type;
            public DnsQueryClass Class;

            public override string ToString() => $"Id: {Id}, Name: {Name}, Type: {Type}, Class: {Class}";
        }

        private static MessageId ToMessageId(DnsHeader message)
        {
            return new MessageId
            {
                Id = message.Id,
                Name = message.Host,
                Type = message.QueryType,
                Class = message.QueryClass
            };
        }

        private ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>>();
        private readonly ILogger<DnsUdpClient> _logger;
        private readonly Socket _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(IPAddress address) :
            this(new NullLogger<DnsUdpClient>(), address)
        {
        }

        public DnsUdpClient(ILogger<DnsUdpClient> logger, IPAddress address)
        {
            _logger = logger;
            _socket.Connect(address, 53);
            _task = Task.Run(RecieveTask);
        }

        private async Task RecieveTask()
        {
            var buffer = new byte[1024];
            var read = 0;

            while (!_cancel.IsCancellationRequested)
            {
                try
                {
                    read = await _socket.ReceiveAsync(buffer, SocketFlags.None, _cancel.Token);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Recieved bad network response from {0}: {1}", _socket.RemoteEndPoint.ToString(), buffer.Take(read).ToDebugString());
                    continue;
                }

                if (read > 0)
                {
                    _ = Task.Run(() => Receive(buffer.ReadBytes(read)));
                }
            }
        }

        public void Receive(byte[] buffer)
        {
            DnsAnswer answer;
            try
            {
                answer = buffer.FromBytes<DnsAnswer>();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Recieved bad DNS response from {0}: {1}", _socket.RemoteEndPoint.ToString(), buffer);
                return;
            }

            if (_pending.TryRemove(ToMessageId(answer.Header), out TaskCompletionSource<byte[]> completionSource))
            {
                completionSource.SetResult(buffer);
            }
        }

        private async Task RemoveFailedRequest(MessageId messageId, CancellationToken token)
        {
            var timeout = TimeSpan.FromSeconds(2);

            await Task.Delay(timeout, token);

            if (_pending.TryRemove(messageId, out TaskCompletionSource<byte[]> completionSource))
            {
                _logger.LogError("Timed out DNS request for {0} from {1}", messageId, _socket.RemoteEndPoint.ToString());
                completionSource.SetException(new DnsClientTimeoutException(timeout, messageId.Name));
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] raw, CancellationToken token)
        {
            _ = RemoveFailedRequest(messageId, token);
            _ = _socket.SendAsync(raw, SocketFlags.None, token);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var raw = query.ToBytes().ToArray();

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, raw, token));

            var result = await completionSource.Task;

            var answer = result.FromBytes<DnsAnswer>();

            // Copy the same ID from the request
            answer.Header.Id = query.Id;

            return answer;
        }

        public void Dispose()
        {
            _cancel.Cancel();
            _task.GetAwaiter().GetResult();

            try
            {
                _socket.Disconnect(false);
            }
            catch (Exception)
            {
            }
        }
    }
}
