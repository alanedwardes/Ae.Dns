using System;

namespace Ae.Dns.Client.Exceptions
{
    /// <summary>
    /// An exception thrown when a DNS request times out.
    /// </summary>
    public sealed class DnsClientTimeoutException : DnsClientException
    {
        /// <summary>
        /// Construct a new <see cref="DnsClientTimeoutException"/> using the specified timeout and domain name.
        /// </summary>
        /// <param name="timeout">The timeout for this request.</param>
        /// <param name="domain">The related DNS query.</param>
        public DnsClientTimeoutException(TimeSpan timeout, string domain)
            : base($"Query timed out after {timeout}s", domain)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// The timeout for this request.
        /// </summary>
        public TimeSpan Timeout { get; }
    }
}
