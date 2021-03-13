using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsIpAddressResource : IDnsResource, IEquatable<DnsIpAddressResource>
    {
        public IPAddress IPAddress { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsIpAddressResource other) => IPAddress.Equals(other.IPAddress);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsIpAddressResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(IPAddress);

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset, int length) => IPAddress = new IPAddress(bytes.ReadBytes(length, ref offset));

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return IPAddress.GetAddressBytes();
        }
    }
}
