using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a client capable of accepting a contiguous region of memory,
    /// reading the query from it, and writing the answer back into it.
    /// </summary>
    public interface IDnsRawClient : IDisposable
    {
        /// <summary>
        /// Reads the number of bytes specified from the buffer, and writes back an answer to the same buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the query from, and write the answer to.</param>
        /// <param name="queryLength">The length of the query in the buffer, in bytes.</param>
        /// <param name="querySource">The source address of the query, for statistics.</param>
        /// <param name="token">The cancellation token to stop the operation.</param>
        /// <returns>The number of bytes written back into the buffer.</returns>
        Task<DnsRawClientResponse> Query(Memory<byte> buffer, int queryLength, EndPoint querySource, CancellationToken token = default);
    }

    public readonly struct DnsRawClientResponse
    {
        public DnsRawClientResponse(int answerLength, DnsMessage query, DnsMessage answer)
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
