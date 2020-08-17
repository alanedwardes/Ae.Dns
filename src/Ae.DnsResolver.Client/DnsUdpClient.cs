using Ae.DnsResolver.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
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
        private readonly UdpClient _client;
        private readonly string _label;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(ILogger<DnsUdpClient> logger, UdpClient client, string label)
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
                var result = await _client.ReceiveAsync();

                var offset = 0;

                DnsResponseMessage answer;
                try
                {
                    answer = DnsMessageReader.ReadDnsResponse(result.Buffer, ref offset);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Recieved bad DNS response from {0}", _label);
                    continue;
                }

                if (_pending.TryRemove(ToMessageId(answer), out TaskCompletionSource<byte[]> completionSource))
                {
                    completionSource.SetResult(result.Buffer);
                }
            }
        }

        private async Task RemoveFailedRequest(MessageId messageId)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            if (_pending.TryRemove(messageId, out TaskCompletionSource<byte[]> completionSource))
            {
                _logger.LogError("Timed out DNS request for {0} from {1}", messageId, _label);
                completionSource.SetException(new TaskCanceledException());
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] bytes)
        {
            _ = RemoveFailedRequest(messageId);
            _ = _client.SendAsync(bytes, bytes.Length);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<DnsHeader> Lookup(string name, DnsQueryClass queryClass, DnsQueryType queryType)
        {
            var dnsMessage = new DnsRequestMessage
            {
                Labels = name.Split("."),
                QueryType = queryType,
                QueryClass = queryClass,
                QuestionCount = 1,
                Flags = 1
            };

            return null;
        }

        public async Task<byte[]> LookupRaw(byte[] raw)
        {
            var query = DnsMessageReader.ReadDnsMessage(raw);

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
        }
    }
}
