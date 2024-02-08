using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Represents a file-backed DNS zone.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsZone : IDnsZone
    {
        /// <inheritdoc/>
        public IList<DnsResourceRecord> Records { get; set; } = new List<DnsResourceRecord>();

        /// <inheritdoc/>
        public DnsLabels Origin { get; set; }

        /// <inheritdoc/>
        public TimeSpan DefaultTtl { get; set; }

        /// <inheritdoc/>
        public void SerializeZone(StreamWriter writer)
        {
            writer.WriteLine($"$ORIGIN {Origin}.");
            writer.WriteLine($"$TTL {(int)DefaultTtl.TotalSeconds}");

            // Copy the records
            var records = Records.ToList();

            // Singular SOA must come first
            var soa = records.Single(x => x.Type == DnsQueryType.SOA);

            records.Remove(soa);
            records.Insert(0, soa);

            foreach (var record in records)
            {
                writer.WriteLine(record.ToZone(this));
            }
        }

        /// <inheritdoc/>
        public void DeserializeZone(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine()?.Trim() ?? string.Empty;

                if (line.StartsWith("$ORIGIN"))
                {
                    Origin = line.Substring("$ORIGIN".Length).Trim().Trim('.');
                    continue;
                }

                if (line.StartsWith("$TTL"))
                {
                    DefaultTtl = TimeSpan.FromSeconds(int.Parse(line.Substring("$TTL".Length)));
                    continue;
                }

                var record = new DnsResourceRecord();
                record.FromZone(this, line);
                Records.Add(record);
            }
            
        }

        /// <inheritdoc/>
        public string FromFormattedHost(string host)
        {
            if (host == "@")
            {
                return Origin;
            }
            else if (host.EndsWith("."))
            {
                return host.Substring(0, host.Length - 1);
            }
            else
            {
                return host + "." + Origin;
            }
        }

        /// <inheritdoc/>
        public string ToFormattedHost(string host)
        {
            if (host == Origin)
            {
                return "@";
            }
            else if (host.EndsWith(Origin))
            {
                return host.Substring(0, host.Length - Origin.ToString().Length - 1);
            }
            else
            {
                return host + '.';
            }
        }
    }
}
