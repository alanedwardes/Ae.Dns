using System;
using System.Text;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// A Certification Authority Authorization (CAA) record is a type of DNS
    /// resource record that specifies which certificate authorities (CAs) are
    /// allowed to issue certificates for a domain. The CAA record format is
    /// specified in RFC 6844.
    /// </summary>
    public sealed class DnsCaaResource : IDnsResource, IEquatable<DnsCaaResource>
    {
        /// <summary>
        /// Issuer Critical:  If set to '1', indicates that the corresponding
        /// property tag MUST be understood if the semantics of the CAA record
        /// are to be correctly interpreted by an issuer.
        /// Issuers MUST NOT issue certificates for a domain if the relevant
        /// CAA Resource Record set contains unknown property tags that have
        /// the Critical bit set.
        /// </summary>
        public byte Flag { get; set; }
        /// <summary>
        /// The property identifier, a sequence of US-ASCII characters.
        /// </summary>
        public string? Tag { get; set; }
        /// <summary>
        /// A sequence of octets representing the property value.
        /// </summary>
        public ReadOnlyMemory<byte> Value { get; set; }

        /// <summary>
        /// If this value is known to be a string, return it as ASCII.
        /// </summary>
        public string ValueAsString
        {
            get
            {
#if NETSTANDARD2_0
                return Encoding.ASCII.GetString(Value.ToArray());
#else
                return Encoding.ASCII.GetString(Value.Span);
#endif
            }
        }

        /// <inheritdoc/>
        public bool Equals(DnsCaaResource? other)
        {
            if (other == null)
            {
                return false;
            }

            return Equals(Flag, other.Flag) && Equals(Tag, other.Tag) && Value.Span.SequenceEqual(other.Value.Span);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DnsCaaResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Flag, Tag, Value);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            var bufferEndPosition = offset + length;
            Flag = bytes.Span[offset++];
            Tag = DnsByteExtensions.ReadStringWithLengthPrefix(bytes, ref offset);
            Value = bytes.Slice(offset, bufferEndPosition - offset);
            offset = bufferEndPosition;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Flag} {Tag} {ValueAsString}";

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            bytes.Span[offset++] = Flag;
            DnsByteExtensions.ToBytes(Tag ?? string.Empty, bytes, ref offset);
            Value.CopyTo(bytes.Slice(offset));
            offset += Value.Length;
        }
    }
}
