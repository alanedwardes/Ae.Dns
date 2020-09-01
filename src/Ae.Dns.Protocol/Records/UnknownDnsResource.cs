using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol.Resources
{
    public sealed class UnknownDnsResource : IDnsResource, IEquatable<UnknownDnsResource>
    {
        public byte[] Raw { get; set; }

        public bool Equals(UnknownDnsResource other) => Raw.SequenceEqual(other.Raw);

        public override bool Equals(object obj) => obj is UnknownDnsResource record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(Raw);

        public void ReadBytes(byte[] bytes, ref int offset, int length) => Raw = bytes.ReadBytes(length, ref offset);

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Raw;
        }
    }
}
