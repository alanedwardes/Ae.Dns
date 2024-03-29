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
        /// Flags to indicate issuer critical options.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// The tag to specify the property represented by this CAA record.
        /// </summary>
        public string Tag { get; set; } = "";

        /// <summary>
        /// The value associated with the tag which can be a hostname or URL.
        /// </summary>
        public string Value { get; set; } = "";

        /// <inheritdoc/>
        public bool Equals(DnsCaaResource? other)
        {
            if (other == null)
            {
                return false;
            }

            return Flags == other.Flags && Tag == other.Tag && Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DnsCaaResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Flags, Tag, Value);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            // Ensure that there is enough data to read the minimum CAA record fields (Flags and Tag Length)
            if (offset + 2 > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Not enough data to read CAA record fields.");
            }

            // Read the Flags byte
            Flags = bytes.Span[offset++];

            // Read the Tag Length byte
            byte tagLength = bytes.Span[offset++];
            if (tagLength == 0 || offset + tagLength > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid tag length or not enough data to read the tag.");
            }

            // Read the Tag bytes
            ReadOnlySpan<byte> tagBytes = bytes.Span.Slice(offset, tagLength);
            Tag = Encoding.ASCII.GetString(tagBytes);
            offset += tagLength;

            // Read the Value bytes
            // Length of the Value field is the remaining length of the enclosing Resource Record data field
            int valueLength = length - 2 - tagLength; // Subtract Flags (1 byte) and Tag Length (1 byte) and Tag
            if (offset + valueLength > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Not enough data to read the value.");
            }

            ReadOnlySpan<byte> valueBytes = bytes.Span.Slice(offset, valueLength);
            Value = Encoding.ASCII.GetString(valueBytes);
            offset += valueLength;
        }

        /// <inheritdoc/>
        public override string ToString() => $"CAA {Flags} {Tag} \"{Value}\"";

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            //DnsByteExtensions.ToBytes(Flags, bytes, ref offset);
            //DnsByteExtensions.ToBytes((byte)Tag.Length, bytes, ref offset);
            //DnsByteExtensions.ToBytes(Tag, bytes, ref offset);
            //DnsByteExtensions.ToBytes(Value, bytes, ref offset);
            // Convert the Tag to ASCII and lowercase since CAA tags are case insensitive
            byte[] tagBytes = Encoding.ASCII.GetBytes(Tag.ToLowerInvariant());
            byte[] valueBytes = Encoding.ASCII.GetBytes(Value);

            // Calculate the total size needed to write the CAA record
            int totalSize = 1 + 1 + tagBytes.Length + valueBytes.Length; // Flags (1 byte), Tag Length (1 byte), Tag, Value

            // Check if the provided buffer is large enough
            if (offset + totalSize > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "The buffer is too small to contain the CAA record data.");
            }

            // Write the Flags byte
            bytes.Span[offset++] = Flags;

            // Write the Tag Length byte
            bytes.Span[offset++] = (byte)tagBytes.Length;

            // Write the Tag bytes
            for (int i = 0; i < tagBytes.Length; i++)
            {
                bytes.Span[offset++] = tagBytes[i];
            }

            // Write the Value bytes
            for (int i = 0; i < valueBytes.Length; i++)
            {
                bytes.Span[offset++] = valueBytes[i];
            }
        }
    }
}