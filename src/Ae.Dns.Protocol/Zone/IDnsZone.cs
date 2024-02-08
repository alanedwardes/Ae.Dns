using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Provides methods to store data against a DNS zone.
    /// </summary>
    public interface IDnsZone
    {
        /// <summary>
        /// Get all records in the zone.
        /// </summary>
        IList<DnsResourceRecord> Records { get; set; }

        /// <summary>
        /// The name of the zone.
        /// </summary>
        DnsLabels Origin { get; set; }

        /// <summary>
        /// The default TTL of the zone.
        /// </summary>
        TimeSpan DefaultTtl { get; set; }

        /// <summary>
        /// Serialize the zone to a <see cref="StreamWriter"/>.
        /// </summary>
        /// <param name="writer"></param>
        void SerializeZone(StreamWriter writer);

        /// <summary>
        /// Deserialize the zone from a <see cref="StreamReader"/>.
        /// </summary>
        /// <param name="reader"></param>
        void DeserializeZone(StreamReader reader);

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
