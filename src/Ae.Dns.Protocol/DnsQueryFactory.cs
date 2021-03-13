using Ae.Dns.Protocol.Enums;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Provides extension methods around <see cref="DnsHeader"/>.
    /// </summary>
    public static class DnsQueryFactory
    {
        /// <summary>
        /// Generate a unique ID to identify this DNS message.
        /// </summary>
        /// <returns>A random <see cref="ushort"/> value.</returns>
        public static ushort GenerateId() => DnsByteExtensions.ReadUInt16(Guid.NewGuid().ToByteArray());

        /// <summary>
        /// Create a DNS query using the specified host name and DNS query type.
        /// </summary>
        /// <param name="host">The DNS host to request in the query.</param>
        /// <param name="type">The type of DNS query to request.</param>
        /// <returns>The complete DNS query.</returns>
        public static DnsHeader CreateQuery(string host, DnsQueryType type = DnsQueryType.A)
        {
            return new DnsHeader
            {
                Id = GenerateId(),
                Host = host,
                QueryType = type,
                QueryClass = DnsQueryClass.IN,
                OperationCode = DnsOperationCode.QUERY,
                QuestionCount = 1,
                RecusionDesired = true
            };
        }

        /// <summary>
        /// Create a reverse DNS query which resolves an IP address to a host name.
        /// </summary>
        /// <param name="ipAddress">The IPv4 or IPv6 address to resolve.</param>
        /// <returns>The correctly formatted <see cref="DnsQueryType.PTR"/> DNS query.</returns>
        public static DnsHeader CreateReverseQuery(IPAddress ipAddress)
        {
            return CreateQuery(GetReverseLookupHostForIpAddress(ipAddress), DnsQueryType.PTR);
        }

        private static string GetReverseLookupHostForIpAddress(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return string.Join(".", ipAddress.ToString().Replace(":", string.Empty).ToCharArray().Reverse()) + ".ip6.arpa";
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return string.Join(".", ipAddress.ToString().Split('.').Reverse()) + ".in-addr.arpa";
            }
            else
            {
                throw new InvalidOperationException($"Invalid address type: {ipAddress.AddressFamily}");
            }
        }
    }
}
