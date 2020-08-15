using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public sealed class DnsUdpClient : IDisposable
    {
        public struct MessageId
        {
            public ushort Id;
            public string Name;
            public DnsQueryType Type;
            public DnsQueryClass Class;
        }

        public static MessageId ToMessageId(DnsHeader message)
        {
            return new MessageId
            {
                Id = message.Id,
                Name = string.Join(".", message.Labels),
                Type = message.Qtype,
                Class = message.Qclass
            };
        }

        private ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>>();

        private readonly UdpClient _client;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(UdpClient client)
        {
            _client = client;
            _task = Task.Run(RecieveTask);
        }

        private async Task RecieveTask()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var result = await _client.ReceiveAsync();

                var offset = 0;
                var message = DnsMessageReader.ReadDnsResponse(result.Buffer, ref offset);

                if (_pending.TryRemove(ToMessageId(message), out TaskCompletionSource<byte[]> completionSource))
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
                completionSource.SetException(new TaskCanceledException());
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] bytes)
        {
            _ = RemoveFailedRequest(messageId);
            _ = _client.SendAsync(bytes, bytes.Length);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<DnsHeader> Lookup(string name, DnsQueryType type)
        {
            var dnsMessage = new DnsRequestMessage
            {
                Labels = name.Split("."),
                Qtype = type,
                Qclass = DnsQueryClass.IN
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
