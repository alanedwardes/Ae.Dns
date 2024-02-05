using Ae.Dns.Client.Zone;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Accepts messages with <see cref="DnsOperationCode.UPDATE"/>, and stores the record for use elsewhere.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsUpdateClient : IDnsClient
    {
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Create the new <see cref="DnsUpdateClient"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="dnsZone"></param>
        public DnsUpdateClient(IDnsZone dnsZone)
        {
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            query.EnsureOperationCode(DnsOperationCode.UPDATE);

            var recordsToAdd = query.Nameservers.Where(x => x.Type == DnsQueryType.A ||
                                                            x.Type == DnsQueryType.AAAA)
                                                .ToArray();

            if (recordsToAdd.Length > 0 && await _dnsZone.AddRecords(recordsToAdd, token))
            {
                return query.CreateAnswerMessage(DnsResponseCode.NoError, ToString());
            }

            return query.CreateAnswerMessage(DnsResponseCode.Refused, ToString());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
