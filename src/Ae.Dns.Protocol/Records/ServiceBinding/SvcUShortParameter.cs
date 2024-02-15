using System;

namespace Ae.Dns.Protocol.Records.ServiceBinding
{
    /// <summary>
    /// A resource containing a single unsigned short.
    /// </summary>
    public sealed class SvcUShortParameter : IDnsResource, IEquatable<SvcUShortParameter>
    {
        /// <summary>
        /// The value.
        /// </summary>
        public ushort Value { get; set; }

        /// <inheritdoc/>
        public bool Equals(SvcUShortParameter? other)
        {
            if (other == null)
            {
                return false;
            }

            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SvcStringParameter record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length) => Value = DnsByteExtensions.ReadUInt16(bytes, ref offset);

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset) => DnsByteExtensions.ToBytes(Value, bytes, ref offset);

        /// <inheritdoc/>
        public override string ToString() => Value.ToString();
    }
}


