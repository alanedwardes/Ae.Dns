using Ae.DnsResolver.Client.Exceptions;
using Ae.DnsResolver.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
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
                Name = string.Join(".", message.Labels),
                Type = message.QueryType,
                Class = message.QueryClass
            };
        }

        private ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>>();
        private readonly ILogger<DnsUdpClient> _logger;
        private readonly Random _random = new Random();
        private readonly Socket _client;
        private readonly string _label;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(ILogger<DnsUdpClient> logger, Socket client, string label)
        {
            _logger = logger;
            _client = client;
            _label = label;
            _task = Task.Run(RecieveTask);
        }

        private async Task RecieveTask()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var buffer = new byte[1024];
                var read = 0;
                try
                {
                    read = await _client.ReceiveAsync(buffer, SocketFlags.None, _cancel.Token);
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

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] bytes)
        {
            _ = RemoveFailedRequest(messageId);
            _ = _client.SendAsync(bytes, SocketFlags.None);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<byte[]> LookupRaw(string name, DnsQueryType queryType)
        {
            var query = new DnsHeader
            {
                Id = (ushort)_random.Next(0, ushort.MaxValue),
                QueryClass = DnsQueryClass.IN,
                QueryType = queryType,
                RecusionDesired = true,
                Labels = name.Split('.'),
                QuestionCount = 1,
            };

            var queryBytes = query.WriteDnsHeader().ToArray();

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, queryBytes));

            var result = await completionSource.Task;

            // Copy the same ID from the request
            result[0] = queryBytes[0];
            result[1] = queryBytes[1];

            return result;
        }

        public async Task<byte[]> LookupRaw(byte[] raw)
        {
            var offset = 0;
            var query = raw.ReadDnsHeader(ref offset);

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, raw));

            var result = await completionSource.Task;

            // Copy the same ID from the request
            result[0] = raw[0];
            result[1] = raw[1];

            return result;
        }

        public void Dispose()
        {
            _cancel.Cancel();
            _task.GetAwaiter().GetResult();
        }
    }
}
