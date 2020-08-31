using System;
using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsTextRecord : DnsResourceRecord, IEquatable<DnsTextRecord>
    {
        public string Text { get; set; }

        public bool Equals(DnsTextRecord other) => Text == other.Text;

        public override bool Equals(object obj) => obj is DnsTextRecord record ? Equals(record) : base.Equals(obj);

        protected override void ReadBytes(byte[] bytes, ref int offset)
        {
            Text = string.Join('.', bytes.ReadString(ref offset, offset + DataLength));
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Text.Split('.').ToBytes();
        }
    }
}
