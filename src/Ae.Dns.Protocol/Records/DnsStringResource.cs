using System;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS resource containing a string.
    /// </summary>
    public abstract class DnsStringResource : IDnsResource, IEquatable<DnsStringResource>
    {
        /// <summary>
        /// The text entry contained within this resource.
        /// </summary>
        /// <value>
        /// The text values of this resource as an array of strings.
        /// </value>
        public string[] Entries { get; set; } = Array.Empty<string>();

        /// <inheritdoc/>
        public bool Equals(DnsStringResource other) => Entries.SequenceEqual(other.Entries);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsStringResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Entries);

        /// <inheritdoc/>
        public virtual void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset, int length)
        {
            Entries = DnsByteExtensions.ReadString(bytes, ref offset, offset + length);
        }

        /// <inheritdoc/>
        public virtual void WriteBytes(Span<byte> bytes, ref int offset)
        {
            DnsByteExtensions.ToBytes(Entries, bytes, ref offset);
        }
    }
}
