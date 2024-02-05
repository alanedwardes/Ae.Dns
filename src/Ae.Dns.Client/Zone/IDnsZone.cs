using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Zone
{
    /// <summary>
    /// Provides methods to store data against a DNS zone.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public interface IDnsZone
    {
        /// <summary>
        /// Add the specified enumerable of <see cref="DnsResourceRecord"/> to this zone.
        /// </summary>
        /// <param name="records"></param>
        /// <param name="token"></param>
        Task<bool> AddRecords(IEnumerable<DnsResourceRecord> records, CancellationToken token = default);

        /// <summary>
        /// Get all records in the zone.
        /// </summary>
        IEnumerable<DnsResourceRecord> Records { get; }
    }
}
