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
        /// <param name="changeDelegate"></param>
        /// <param name="recordsToAdd"></param>
        /// <param name="token"></param>
        Task<bool> ChangeRecords(Action<ICollection<DnsResourceRecord>> changeDelegate, IEnumerable<DnsResourceRecord> recordsToAdd, CancellationToken token = default);

        /// <summary>
        /// Get all records in the zone.
        /// </summary>
        IEnumerable<DnsResourceRecord> Records { get; }

        /// <summary>
        /// The name of the zone.
        /// </summary>
        string Name { get;  }
    }
}
