using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Provides methods to store data against a DNS zone.
    /// </summary>
    public interface IDnsZone
    {
        /// <summary>
        /// Records contained in this zone.
        /// </summary>
        IList<DnsResourceRecord> Records { get; set; }

        /// <summary>
        /// The name of the zone.
        /// </summary>
        DnsLabels Origin { get; set; }

        /// <summary>
        /// The default TTL of the zone.
        /// </summary>
        TimeSpan? DefaultTtl { get; set; }

        /// <summary>
        /// Update the DNS zone; allows implementation-specific concurrency control.
        /// </summary>
        /// <param name="modification"></param>
        /// <returns></returns>
        Task<TResult> Update<TResult>(Func<TResult> modification);
    }
}
