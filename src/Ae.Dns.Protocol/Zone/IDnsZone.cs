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
        /// Get all records in the zone.
        /// Do not modify records outside of the update method.
        /// </summary>
        IReadOnlyList<DnsResourceRecord> Records { get; }

        /// <summary>
        /// The name of the zone.
        /// </summary>
        DnsLabels Origin { get; set; }

        /// <summary>
        /// The default TTL of the zone.
        /// </summary>
        TimeSpan DefaultTtl { get; set; }

        /// <summary>
        /// Called when the zone is updated.
        /// Guaranteed to never be invoked in parallel.
        /// </summary>
        Func<IDnsZone, Task> ZoneUpdated { set; }

        /// <summary>
        /// Update the DNS zone; only one update can happen simultaneously.
        /// </summary>
        /// <param name="modification"></param>
        /// <returns></returns>
        Task Update(Action<IList<DnsResourceRecord>> modification);

        /// <summary>
        /// Serialize the zone to a string.
        /// </summary>
        string SerializeZone();

        /// <summary>
        /// Deserialize the zone from a string.
        /// </summary>
        /// <param name="zone"></param>
        void DeserializeZone(string zone);

        /// <summary>
        /// Format a host from the zone file format.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        string FromFormattedHost(string host);

        /// <summary>
        /// Format a host name for a zone file.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        string ToFormattedHost(string host);
    }
}
