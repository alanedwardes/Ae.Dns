using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS resource containing a string.
    /// </summary>
    public sealed class DnsTextResource : IDnsResource, IEquatable<DnsTextResource>
    {
        /// <summary>
        /// The text entry contained within this resource.
        /// </summary>
        /// <value>
        /// The text value of this resource as a single entry, joined together with periods (.)
        /// </value>
        public string Text { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsTextResource other) => Text == other.Text;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsTextResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Text);

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset, int length) => Text = string.Join(".", bytes.ReadString(ref offset, offset + length));

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Text.Split('.').ToBytes();
        }

        /// <inheritdoc/>
        public override string ToString() => Text;
    }
}
