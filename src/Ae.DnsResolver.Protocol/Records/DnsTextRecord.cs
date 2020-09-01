using System;
using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsTextRecord : DnsResourceRecord, IEquatable<DnsTextRecord>
    {
        public string Text { get; set; }

        public bool Equals(DnsTextRecord other) => Text == other.Text && base.Equals(other);

        public override bool Equals(object obj) => obj is DnsTextRecord record ? Equals(record) : base.Equals(obj);

        protected override void ReadBytes(byte[] bytes, ref int offset, int expectedLength)
        {
            Text = string.Join('.', bytes.ReadString(ref offset, offset + expectedLength));
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Text.Split('.').ToBytes();
        }
    }
}
