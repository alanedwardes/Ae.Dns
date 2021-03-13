using System;

namespace Ae.Dns.Client.Exceptions
{
    /// <summary>
    /// An exception thrown when a DNS request times out.
    /// </summary>
    public sealed class DnsClientTimeoutException : Exception
    {
        /// <summary>
        /// Construct a new <see cref="DnsClientTimeoutException"/> using the specified timeout and domain name.
        /// </summary>
        /// <param name="timeout">The timeout for this request.</param>
        /// <param name="domain">The domain which timed out.</param>
        public DnsClientTimeoutException(TimeSpan timeout, string domain)
            : base($"The following query timed out after {timeout}s: {domain}")
        {
            Timeout = timeout;
            Domain = domain;
        }

        /// <summary>
        /// The timeout for this request.
        /// </summary>
        public TimeSpan Timeout { get; }
        /// <summary>
        /// The domain which timed out.
        /// </summary>
        public string Domain { get; }
    }
}
