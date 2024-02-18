using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>> _pending = new ConcurrentDictionary<MessageId, TaskCompletionSource<byte[]>>();
        private readonly ILogger<DnsUdpClient> _logger;
        private readonly DnsUdpClientOptions _options;
        private readonly Socket _socket;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// Construct a new <see cref="DnsUdpClient"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        [ActivatorUtilitiesConstructor]
        public DnsUdpClient(ILogger<DnsUdpClient> logger, IOptions<DnsUdpClientOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _socket = new Socket(options.Value.Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _task = Task.Run(ReceiveTask);
        }

        /// <summary>
        /// Construct a new <see cref="DnsUdpClient"/>.
        /// </summary>
        /// <param name="options"></param>
        public DnsUdpClient(DnsUdpClientOptions options)
            : this(NullLogger<DnsUdpClient>.Instance, Options.Create(options))
        {
        }

        private async Task ReceiveTask()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var buffer = DnsByteExtensions.AllocatePinnedNetworkBuffer();

                int recieved;

                try
                {
                    recieved = await _socket.ReceiveAsync(buffer, SocketFlags.None, _cancel.Token);
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

        private void Receive(ReadOnlyMemory<byte> buffer)
        {
            DnsMessage answer;
            try
            {
                answer = DnsByteExtensions.FromBytes<DnsMessage>(buffer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Received bad DNS response from {0}: {1}", _options.Endpoint, buffer);
                return;
            }

            if (_pending.TryRemove(ToMessageId(answer), out TaskCompletionSource<byte[]> completionSource))
            {
                completionSource.SetResult(buffer.ToArray());
            }
        }

        private async Task RemoveFailedRequest(MessageId messageId, CancellationToken token)
        {
            Exception exception = new DnsClientTimeoutException(_options.Timeout, messageId.Name);
            try
            {
                await Task.Delay(_options.Timeout, token);
            }
            catch (OperationCanceledException e)
            {
                // Ensure cancellation does complete the task
                exception = e;
            }

            if (_pending.TryRemove(messageId, out TaskCompletionSource<byte[]> completionSource))
            {
                _logger.LogError("Timed out DNS request for {0} from {1} in {2}", messageId, _options.Endpoint, _options.Timeout);
                completionSource.SetException(exception);
            }
        }

        private TaskCompletionSource<byte[]> SendQueryInternal(MessageId messageId, ReadOnlyMemory<byte> raw, CancellationToken token)
        {
            _ = RemoveFailedRequest(messageId, token);
            _ = _socket.SendToAsync(raw, SocketFlags.None, _options.Endpoint, token);
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
            answer.Header.Tags.Add("Upstream", _options.Endpoint);
            return answer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _cancel.Cancel();
                _socket.Close();
                _task.GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // Prevent Dispose from throwing
            }

            _socket.Dispose();
            _cancel.Dispose();
        }

        /// <inheritdoc/>
        public override string ToString() => $"udp://{_options.Endpoint}/";
    }
}
