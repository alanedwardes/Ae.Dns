using System;
using System.Linq;

namespace Ae.Dns.Protocol.Records.ServiceBinding
{
    /// <summary>
    /// See https://www.ietf.org/archive/id/draft-ietf-dnsop-svcb-https-12.html#name-alpn-and-no-default-alpn
    /// </summary>
    public sealed class SvcStringParameter : IDnsResource, IEquatable<SvcStringParameter>
    {
        /// <summary>
        /// The string entries associated with this parameter.
        /// </summary>
        public DnsLabels Entries { get; set; }

        /// <inheritdoc/>
        public bool Equals(SvcStringParameter? other)
        {
            if (other == null)
            {
                return false;
            }

            return Entries.SequenceEqual(other.Entries);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SvcStringParameter record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Entries);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            var stringLength = 0;
            Entries = DnsByteExtensions.ReadString(bytes.Slice(offset, length), ref stringLength, false);
            // stringLength must be the same as length at this point
            offset += stringLength;
        }

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            foreach (var entry in Entries)
            {
                DnsByteExtensions.ToBytes(entry, bytes, ref offset);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => Entries;
    }
}
