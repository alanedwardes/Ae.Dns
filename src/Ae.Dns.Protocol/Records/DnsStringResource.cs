using System;
using System.Collections.Generic;
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
        public IList<string> Entries { get; set; } = Array.Empty<string>();

        /// <inheritdoc/>
        public bool Equals(DnsStringResource other) => Entries.SequenceEqual(other.Entries);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsStringResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Entries);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset, int length)
        {
            Entries = DnsByteExtensions.ReadString(bytes, ref offset, offset + length);
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return DnsByteExtensions.ToBytes(Entries);
        }
    }
}
