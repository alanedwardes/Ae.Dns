using Ae.Dns.Protocol.Zone;
using System;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS text resource containing a string.
    /// </summary>
    public sealed class DnsTextResource : DnsStringResource
    {
        /// <inheritdoc/>
        protected override bool CanUseCompression => false;

        /// <inheritdoc/>
        public override string ToZone(IDnsZone zone)
        {
            if (Entries.Count == 1)
            {
                return Entries.Single();
            }

            return string.Join(" ", Entries.Select(x => $"\"{x}\""));
        }

        /// <inheritdoc/>
        public override void FromZone(IDnsZone zone, string input)
        {
            var parts = input.Split('"').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            Entries = new DnsLabels(parts);
        }

        /// <inheritdoc/>
        public override string ToString() => Entries;
    }
}
