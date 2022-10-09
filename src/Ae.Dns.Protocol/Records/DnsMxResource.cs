using Ae.Dns.Protocol.Enums;
using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a mail server DNS record. See <see cref="DnsQueryType.MX"/>.
    /// </summary>
    public sealed class DnsMxResource : IDnsResource, IEquatable<DnsMxResource>
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
        public string Exchange { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsMxResource other) => Preference == other.Preference && Exchange == other.Exchange;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsMxResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Preference, Exchange);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset, int length)
        {
            Preference = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            Exchange = string.Join(".", DnsByteExtensions.ReadString(bytes, ref offset));
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return DnsByteExtensions.ToBytes(Preference);
            yield return DnsByteExtensions.ToBytes(Exchange.Split('.'));
        }

        /// <inheritdoc/>
        public override string ToString() => Exchange.ToString();
    }
}
