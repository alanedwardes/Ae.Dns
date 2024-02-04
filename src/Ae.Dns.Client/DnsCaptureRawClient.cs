using Ae.Dns.Protocol;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Defines a client which can be set to capture DNS packets.
    /// </summary>
    public sealed class DnsCaptureRawClient : IDnsRawClient
    {
        private readonly IDnsRawClient _innerClient;

        /// <summary>
        /// The raw bytes for the capture.
        /// </summary>
        public sealed class Capture
        {
            /// <summary>
            /// The incoming request.
            /// </summary>
            public DnsRawClientRequest Request { get; set; }
            /// <summary>
            /// The raw query.
            /// </summary>
            public ReadOnlyMemory<byte> Query { get; set; }
            /// <summary>
            /// The raw answer.
            /// </summary>
            public ReadOnlyMemory<byte>? Answer { get; set; }
            /// <summary>
            /// The outgoing response.
            /// </summary>
            public DnsRawClientResponse? Response { get; set; }
            /// <summary>
            /// An exception, if there was one.
            /// </summary>
            public Exception Exception { get; set; }
        }

        /// <summary>
        /// Whether packet capture is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// An optional filter for the packet capture.
        /// </summary>
        public Func<DnsRawClientRequest, DnsRawClientResponse, bool> CaptureFilter { get; set; } = (request, response) => true;

        /// <summary>
        /// The raw captures.
        /// </summary>
        public ConcurrentBag<Capture> Captures { get; } = new ConcurrentBag<Capture>();

        /// <summary>
        /// Construct a new (inactive) packet capturing client, delegating to the specified <see cref="IDnsRawClient"/>.
        /// </summary>
        /// <param name="innerClient"></param>
        public DnsCaptureRawClient(IDnsRawClient innerClient)
        {
            _innerClient = innerClient;
        }

        /// <inheritdoc/>
        public void Dispose() => _innerClient.Dispose();

        /// <inheritdoc/>
        public async Task<DnsRawClientResponse> Query(Memory<byte> buffer, DnsRawClientRequest request, CancellationToken token = default)
        {
            if (!IsEnabled)
            {
                return await _innerClient.Query(buffer, request, token);
            }

            // Unfortunately we must copy the query before
            // we know whether we need it, otherwise it will
            // overwritten in the buffer by the answer.
            var queryBuffer = new byte[request.QueryLength];
            buffer.Slice(0, request.QueryLength).CopyTo(queryBuffer);

            DnsRawClientResponse response;
            try
            {
                response = await _innerClient.Query(buffer, request, token);
            }
            catch (Exception ex)
            {
                Captures.Add(new Capture
                {
                    Request = request,
                    Query = queryBuffer,
                    Exception = ex
                });
                throw;
            }

            if (CaptureFilter(request, response))
            {
                var answerBuffer = new byte[response.AnswerLength];
                buffer.Slice(0, response.AnswerLength).CopyTo(answerBuffer);

                Captures.Add(new Capture
                {
                    Request = request,
                    Query = queryBuffer,
                    Answer = answerBuffer,
                    Response = response
                });
            }

            return response;
        }
    }
}
