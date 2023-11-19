using System;
using System.Text;

namespace Ae.Dns.Protocol.Records.ServiceBinding
{
    /// <summary>
    /// See https://www.ietf.org/archive/id/draft-ietf-add-svcb-dns-09.html#name-dohpath for an example usage.
    /// </summary>
    public sealed class SvcUtf8StringParameter : IDnsResource, IEquatable<SvcUtf8StringParameter>
    {
        /// <summary>
        /// The string entry associated with this parameter.
        /// </summary>
        public string? Value { get; set; }

        /// <inheritdoc/>
        public bool Equals(SvcUtf8StringParameter? other)
        {
            if (other == null)
            {
                return false;
            }

            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SvcUtf8StringParameter record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            Value = Encoding.UTF8.GetString(bytes.Slice(offset, length).ToArray());
            offset += length;
        }

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            var stringBytes = Encoding.UTF8.GetBytes(Value ?? throw new NullReferenceException("No string was set to write"));
            stringBytes.CopyTo(bytes.Slice(offset));
            offset += stringBytes.Length;
        }

        /// <inheritdoc/>
        public override string? ToString() => Value;
    }
}
