using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsIpAddressRecord : DnsResourceRecord, IEquatable<DnsIpAddressRecord>
    {
        public IPAddress IPAddress { get; set; }

        public bool Equals(DnsIpAddressRecord other) => IPAddress.Equals(other.IPAddress);

        public override bool Equals(object obj) => obj is DnsIpAddressRecord record ? Equals(record) : base.Equals(obj);

        protected override void ReadBytes(byte[] bytes, ref int offset, int expectedLength) => IPAddress = new IPAddress(bytes.ReadBytes(expectedLength, ref offset));

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return IPAddress.GetAddressBytes();
        }
    }
}
