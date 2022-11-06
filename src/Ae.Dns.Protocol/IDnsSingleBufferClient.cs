using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a client capable of accepting a contiguous region of memory,
    /// reading the query from it, and writing the answer back into it.
    /// </summary>
    public interface IDnsSingleBufferClient : IDisposable
    {
        /// <summary>
        /// Reads the number of bytes specified from the buffer, and writes back an answer to the same buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the query from, and write the answer to.</param>
        /// <param name="queryLength">The length of the query in the buffer, in bytes.</param>
        /// <param name="token">The cancellation token to stop the operation.</param>
        /// <returns>The number of bytes written back into the buffer.</returns>
        Task<DnsSingleBufferClientResponse> Query(Memory<byte> buffer, int queryLength, CancellationToken token = default);
    }

    public readonly struct DnsSingleBufferClientResponse
    {
        public DnsSingleBufferClientResponse(int answerLength, DnsMessage query, DnsMessage answer)
        {
            AnswerLength = answerLength;
            Query = query;
            Answer = answer;
        }

        public readonly int AnswerLength;
        public readonly DnsMessage Query;
        public readonly DnsMessage Answer;
    }
}
