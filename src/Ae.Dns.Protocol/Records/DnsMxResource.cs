using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Zone;
using System;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a mail server DNS record. See <see cref="DnsQueryType.MX"/>.
    /// </summary>
    public sealed class DnsMxResource : DnsStringResource, IEquatable<DnsMxResource>
    {
        /// <summary>
        /// The priority or preference field identifies which mailserver should be preferred.
        /// </summary>
        /// <value>
        /// The priority field identifies which mailserver should be preferred - if multiple
        /// values are the same, mail would be expected to flow evenly to all hosts.
        /// </value>
        public ushort Preference { get; set; }

        /// <summary>
        /// The domain name of a mailserver. 
        /// </summary>
        /// <value>
        /// The host name must map directly to one or more address records (A, or AAAA) in the DNS, and must not point to any CNAME records.
        /// </value>
        public string Exchange => Entries;

        /// <inheritdoc/>
        protected override bool CanUseCompression => true;

        /// <inheritdoc/>
        public bool Equals(DnsMxResource? other)
        {
            if (other == null)
            {
                return false;
            }

            return Preference == other.Preference && Exchange == other.Exchange;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DnsMxResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Preference, Exchange);

        /// <inheritdoc/>
        public override void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            Preference = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            base.ReadBytes(bytes, ref offset, length - sizeof(ushort));
        }

        /// <inheritdoc/>
        public override void FromZone(IDnsZone zone, string input)
        {
            var parts = input.Split(null);
            Preference = ushort.Parse(parts[0]);
            Entries = zone.FromFormattedHost(parts[1]);
        }

        /// <inheritdoc/>
        public override string ToZone(IDnsZone zone)
        {
            return $"{Preference} {zone.ToFormattedHost(Entries)}";
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Preference} {Entries}";

        /// <inheritdoc/>
        public override void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            DnsByteExtensions.ToBytes(Preference, bytes, ref offset);
            base.WriteBytes(bytes, ref offset);
        }
    }
}
