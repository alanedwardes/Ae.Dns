using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public sealed class DnsUdpClient : IDisposable
    {
        public struct RecordType
        {
            public string Name;
            public DnsQueryType Type;
            public DnsQueryClass Class;
        }

        public static RecordType ToRecordType(DnsMessage message)
        {
            return new RecordType
            {
                Name = string.Join(".", message.Labels),
                Type = message.Qtype,
                Class = message.Qclass
            };
        }

        private static ConcurrentDictionary<RecordType, TaskCompletionSource<byte[]>> _cache = new ConcurrentDictionary<RecordType, TaskCompletionSource<byte[]>>();

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

                if (_cache.TryGetValue(ToRecordType(message), out var completionSource))
                {
                    completionSource.SetResult(result.Buffer);
                }
            }
        }

        public Task<DnsMessage> Lookup(string name, DnsQueryType type)
        {
            var dnsMessage = new DnsRequestMessage();
            dnsMessage.Labels = name.Split(".");
            dnsMessage.Qtype = type;
            dnsMessage.Qclass = DnsQueryClass.IN;

            return null;
        }

        public async Task<byte[]> LookupRaw(byte[] raw)
        {
            var message = DnsMessageReader.ReadDnsMessage(raw);

            var completionSource = _cache.GetOrAdd(ToRecordType(message), key =>
            {
                _client.SendAsync(raw, raw.Length);
                return new TaskCompletionSource<byte[]>();
            });

            return await completionSource.Task;
        }

        public void Dispose()
        {
            _cancel.Cancel();
        }
    }
}
