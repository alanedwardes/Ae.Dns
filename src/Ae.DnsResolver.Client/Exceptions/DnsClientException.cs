using System;

namespace Ae.DnsResolver.Client.Exceptions
{
    public sealed class DnsClientTimeoutException : Exception
    {
        public DnsClientTimeoutException(TimeSpan timeout, string domain) : base($"The following query timed out after {timeout}s: {domain}")
        {
            Timeout = timeout;
            Domain = domain;
        }

        public TimeSpan Timeout { get; }
        public string Domain { get; }
    }
}
