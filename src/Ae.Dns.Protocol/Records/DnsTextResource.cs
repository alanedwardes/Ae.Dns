using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Resources
{
    public sealed class DnsTextResource : IDnsResource, IEquatable<DnsTextResource>
    {
        public string Text { get; set; }

        public bool Equals(DnsTextResource other) => Text == other.Text;

        public override bool Equals(object obj) => obj is DnsTextResource record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(Text);

        public void ReadBytes(byte[] bytes, ref int offset, int length) => Text = string.Join(".", bytes.ReadString(ref offset, offset + length));

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Text.Split('.').ToBytes();
        }
    }
}
