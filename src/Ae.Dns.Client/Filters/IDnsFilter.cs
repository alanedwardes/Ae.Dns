using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Represents a class which can permit/deny DNS queries.
    /// </summary>
    public interface IDnsFilter
    {
        /// <summary>
        /// Returns true if the DNS query is permitted.
        /// </summary>
        public bool IsPermitted(DnsHeader query);
    }
}
