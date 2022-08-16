using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client which prevents an upstream from returning a DNS record which contains a private IP address.
    /// </summary>
    public sealed class DnsRebindMitigationClient : IDnsClient
    {
        private readonly ILogger<DnsRebindMitigationClient> _logger;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsRebindMitigationClient"/> using the specified <see cref="IDnsClient"/>.
        /// </summary>
        public DnsRebindMitigationClient(IDnsClient dnsClient) : this(NullLogger<DnsRebindMitigationClient>.Instance, dnsClient)
        {
        }

        /// <summary>
        /// Construct a new <see cref="DnsRebindMitigationClient"/> using the specified <see cref="ILogger"/> and <see cref="IDnsClient"/>.
        /// </summary>
        public DnsRebindMitigationClient(ILogger<DnsRebindMitigationClient> logger, IDnsClient dnsClient)
        {
            _logger = logger;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            var answer = await _dnsClient.Query(query, token);

            var ipAddressResponses = answer.Answers.Where(x => x.Type == DnsQueryType.A || x.Type == DnsQueryType.AAAA)
                .Select(x => x.Resource)
                .Cast<DnsIpAddressResource>()
                .Select(x => x.IPAddress);

            if (ipAddressResponses.Any(IsPrivate))
            {
                _logger.LogWarning("DNS rebind attack mitigated for {query}", query);
                return CreateNullHeader(query);
            }

            return answer;
        }

        private bool IsPrivate(IPAddress ip)
        {
            // Map back to IPv4 if mapped to IPv6, for example "::ffff:1.2.3.4" to "1.2.3.4".
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            // Checks loopback ranges for both IPv4 and IPv6.
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            // IPv4
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return IsPrivateIPv4(ip.GetAddressBytes());
            }

            // IPv6
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return ip.IsIPv6LinkLocal ||
#if NET6_0
                       ip.IsIPv6UniqueLocal ||
#endif
                       ip.IsIPv6SiteLocal;
            }

            throw new NotSupportedException(
                    $"IP address family {ip.AddressFamily} is not supported, expected only IPv4 (InterNetwork) or IPv6 (InterNetworkV6).");
        }

        private static bool IsPrivateIPv4(byte[] ipv4Bytes)
        {
            // Link local (no IP assigned by DHCP): 169.254.0.0 to 169.254.255.255 (169.254.0.0/16)
            bool IsLinkLocal() => ipv4Bytes[0] == 169 && ipv4Bytes[1] == 254;

            // Class A private range: 10.0.0.0 – 10.255.255.255 (10.0.0.0/8)
            bool IsClassA() => ipv4Bytes[0] == 10;

            // Class B private range: 172.16.0.0 – 172.31.255.255 (172.16.0.0/12)
            bool IsClassB() => ipv4Bytes[0] == 172 && ipv4Bytes[1] >= 16 && ipv4Bytes[1] <= 31;

            // Class C private range: 192.168.0.0 – 192.168.255.255 (192.168.0.0/16)
            bool IsClassC() => ipv4Bytes[0] == 192 && ipv4Bytes[1] == 168;

            return IsLinkLocal() || IsClassA() || IsClassC() || IsClassB();
        }

        private DnsMessage CreateNullHeader(DnsMessage query) => new DnsMessage
        {
            Header = new DnsHeader
            {
                Id = query.Header.Id,
                ResponseCode = DnsResponseCode.Refused,
                IsQueryResponse = true,
                RecursionAvailable = true,
                RecusionDesired = query.Header.RecusionDesired,
                Host = query.Header.Host,
                QueryClass = query.Header.QueryClass,
                QuestionCount = query.Header.QuestionCount,
                QueryType = query.Header.QueryType
            }
        };
    }
}
