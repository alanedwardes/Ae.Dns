using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class UnimplementedDnsResourceRecord : DnsResourceRecord, IEquatable<UnimplementedDnsResourceRecord>
    {
        public byte[] Raw { get; set; }

        public bool Equals(UnimplementedDnsResourceRecord other) => Raw.SequenceEqual(other.Raw) && base.Equals(other);

        public override bool Equals(object obj) => obj is UnimplementedDnsResourceRecord record ? Equals(record) : base.Equals(obj);

        protected override void ReadBytes(byte[] bytes, ref int offset, int expectedLength) => Raw = bytes.ReadBytes(expectedLength, ref offset);

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Raw;
        }
    }
}
