using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Zone;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Accepts messages with <see cref="DnsOperationCode.UPDATE"/>, and stores the record for use elsewhere.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsZoneUpdateClient : IDnsClient
    {
        private readonly ILogger<DnsZoneUpdateClient> _logger;
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Create the new <see cref="DnsZoneUpdateClient"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dnsZone"></param>
        public DnsZoneUpdateClient(ILogger<DnsZoneUpdateClient> logger, IDnsZone dnsZone)
        {
            _logger = logger;
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            query.EnsureOperationCode(DnsOperationCode.UPDATE);
            query.EnsureQueryType(DnsQueryType.SOA);
            query.EnsureHost(_dnsZone.Origin);

            // Early out without requiring a lock (e.g. in the zone update)
            var firstPreReqsCheckResponseCode = _dnsZone.TestZoneUpdatePreRequisites(query);
            if (firstPreReqsCheckResponseCode != DnsResponseCode.NoError)
            {
                return query.CreateAnswerMessage(firstPreReqsCheckResponseCode, ToString());
            }

            // Run the zone update and return the response
            return query.CreateAnswerMessage(await _dnsZone.Update(records =>
            {
                // The update method locks, it's unlikely but things could have changed since
                // we ran the first pre-reqs check (given that can happen in parallel)
                var responseCode = _dnsZone.TestZoneUpdatePreRequisites(query);
                if (responseCode != DnsResponseCode.NoError)
                {
                    return responseCode;
                }

                // Run the updates against the zone.
                return _dnsZone.PerformZoneUpdates(records, query);
            }), ToString());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsZoneUpdateClient)}({_dnsZone})";
    }
}
