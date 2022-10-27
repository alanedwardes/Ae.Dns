using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
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

        private static MessageId ToMessageId(DnsMessage message)
        {
            return new MessageId
            {
                Id = message.Header.Id,
                Name = message.Header.Host,
                Type = message.Header.QueryType,
                Class = message.Header.QueryClass
            };
        }

        private ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>>();
        private readonly ILogger<DnsUdpClient> _logger;
        private readonly IPEndPoint _endpoint;
        private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Task _task;
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
            _endpoint = endpoint;
            _task = Task.Run(ReceiveTask);
        }

        private async Task ReceiveTask()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

                int recieved;

                try
                {
                    recieved = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing, client shutting down
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Received bad network response");
                    continue;
                }

                if (recieved > 0)
                {
                    _ = Task.Run(() => Receive(buffer.Slice(0, recieved)));
                }
            }
        }

#if NETSTANDARD2_1
        private void Receive(ArraySegment<byte> buffer)
#else
        private void Receive(Memory<byte> buffer)
#endif
        {
            DnsMessage answer;
            try
            {
#if NETSTANDARD2_1
                answer = DnsByteExtensions.FromBytes<DnsMessage>(buffer);
#else
                answer = DnsByteExtensions.FromBytes<DnsMessage>(buffer.Span);
#endif
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Received bad DNS response from {0}: {1}", _endpoint, buffer);
                return;
            }

            if (_pending.TryRemove(ToMessageId(answer), out TaskCompletionSource<byte[]> completionSource))
            {
                completionSource.SetResult(buffer.ToArray());
            }
        }

        private async Task RemoveFailedRequest(MessageId messageId, CancellationToken token)
        {
            var timeout = TimeSpan.FromSeconds(2);

            Exception exception = new DnsClientTimeoutException(timeout, messageId.Name);
            try
            {
                await Task.Delay(timeout, token);
            }
            catch (OperationCanceledException e)
            {
                // Ensure cancellation does complete the task
                exception = e;
            }

            if (_pending.TryRemove(messageId, out TaskCompletionSource<byte[]> completionSource))
            {
                _logger.LogError("Timed out DNS request for {0} from {1}", messageId, _endpoint);
                completionSource.SetException(exception);
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, byte[] raw, CancellationToken token)
        {
            _ = RemoveFailedRequest(messageId, token);
            _ = _socket.SendToAsync(raw, SocketFlags.None, _endpoint);
            return new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var raw = DnsByteExtensions.AllocateAndWrite(query).ToArray();

            var completionSource = _pending.GetOrAdd(ToMessageId(query), key => SendQueryInternal(key, raw, token));

            var result = await completionSource.Task;

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(result);

            // Copy the same ID from the request
            answer.Header.Id = query.Header.Id;
            answer.Header.Tags.Add("Resolver", ToString());
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

        /// <inheritdoc/>
        public override string ToString() => $"udp://{_endpoint}/";
    }
}
