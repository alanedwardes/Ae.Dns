using Ae.DnsResolver.Client.Exceptions;
using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
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
        private readonly string _label;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(ILogger<DnsUdpClient> logger, IPAddress address, string label)
        {
            _logger = logger;
            _socket.Connect(address, 53);
            _label = label;
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
                    _logger.LogCritical(e, "Recieved bad network response from {0}: {1}", _label, buffer.Take(read).ToDebugString());
                    continue;
                }

                if (read > 0)
                {
                    var offset = 0;
                    _ = Task.Run(() => Receive(buffer.ReadBytes(read, ref offset)));
                }
            }
        }

        public void Receive(byte[] buffer)
        {
            DnsAnswer answer;
            try
            {
                var offset = 0;
                answer = buffer.ReadDnsAnswer(ref offset);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Recieved bad DNS response from {0}: {1}", _label, buffer);
                return;
            }

            if (_pending.TryRemove(ToMessageId(answer.Header), out TaskCompletionSource<byte[]> completionSource))
            {
                completionSource.SetResult(buffer);
            }
        }

        private async Task RemoveFailedRequest(MessageId messageId)
        {
            var timeout = TimeSpan.FromSeconds(2);

            await Task.Delay(timeout);

            if (_pending.TryRemove(messageId, out TaskCompletionSource<byte[]> completionSource))
            {
                _logger.LogError("Timed out DNS request for {0} from {1}", messageId, _label);
                completionSource.SetException(new DnsClientTimeoutException(timeout, messageId.Name));
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] raw)
        {
            _ = RemoveFailedRequest(messageId);
            _ = _socket.SendAsync(raw, SocketFlags.None);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<DnsAnswer> Query(DnsHeader query)
        {
            var raw = query.WriteDnsHeader().ToArray();

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, raw));

            var result = await completionSource.Task;

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

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
