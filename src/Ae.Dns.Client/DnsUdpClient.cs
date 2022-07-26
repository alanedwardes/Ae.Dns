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
    /// <summary>
    /// A DNS UDP client implementation.
    /// </summary>
    public sealed class DnsUdpClient : IDnsClient
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
        private readonly UdpClient _socket;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public DnsUdpClient(IPAddress address) :
            this(new NullLogger<DnsUdpClient>(), address)
        {
        }

        public DnsUdpClient(IPEndPoint endpoint) :
            this(new NullLogger<DnsUdpClient>(), endpoint)
        {
        }

        public DnsUdpClient(ILogger<DnsUdpClient> logger, IPAddress address) :
            this(logger, new IPEndPoint(address, 53))
        {
        }

        public DnsUdpClient(ILogger<DnsUdpClient> logger, IPEndPoint endpoint)
        {
            _logger = logger;
            _socket = new UdpClient(endpoint.AddressFamily);
            _socket.Connect(endpoint);
            _task = Task.Run(ReceiveTask);
        }

        private async Task ReceiveTask()
        {
            while (!_cancel.IsCancellationRequested)
            {
                UdpReceiveResult recieve;

                try
                {
                    recieve = await _socket.ReceiveAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing, client shutting down
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Received bad network response from {0}: {1}", recieve.RemoteEndPoint, DnsByteExtensions.ToDebugString(recieve.Buffer));
                    continue;
                }

                if (recieve.Buffer.Length > 0)
                {
                    _ = Task.Run(() => Receive(recieve.Buffer));
                }
            }
        }

        private void Receive(byte[] buffer)
        {
            DnsAnswer answer;
            try
            {
                answer = DnsByteExtensions.FromBytes<DnsAnswer>(buffer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Received bad DNS response from {0}: {1}", _socket.Client.RemoteEndPoint, buffer);
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
                _logger.LogError("Timed out DNS request for {0} from {1}", messageId, _socket.Client.RemoteEndPoint);
                completionSource.SetException(new DnsClientTimeoutException(timeout, messageId.Name));
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] raw, CancellationToken token)
        {
            _ = RemoveFailedRequest(messageId, token);
            _ = _socket.SendAsync(raw, raw.Length);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var raw = DnsByteExtensions.ToBytes(query).ToArray();

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, raw, token));

            var result = await completionSource.Task;

            var answer = DnsByteExtensions.FromBytes<DnsAnswer>(result);

            // Copy the same ID from the request
            answer.Header.Id = query.Id;

            return answer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cancel.Cancel();
            _socket.Close();
            _task.GetAwaiter().GetResult();
            _socket.Dispose();
        }
    }
}
