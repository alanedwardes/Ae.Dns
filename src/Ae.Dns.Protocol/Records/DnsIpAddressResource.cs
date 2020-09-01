using Ae.Dns.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsIpAddressResource : IDnsResource, IEquatable<DnsIpAddressResource>
    {
        public IPAddress IPAddress { get; set; }

        public bool Equals(DnsIpAddressResource other) => IPAddress.Equals(other.IPAddress);

        public override bool Equals(object obj) => obj is DnsIpAddressResource record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(IPAddress);

        public void ReadBytes(byte[] bytes, ref int offset, int length) => IPAddress = new IPAddress(bytes.ReadBytes(length, ref offset));

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return IPAddress.GetAddressBytes();
        }
    }
}
