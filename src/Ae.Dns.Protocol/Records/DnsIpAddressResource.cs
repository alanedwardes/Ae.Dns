using Ae.Dns.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS resource record which contains an Internet Protocol address.
    /// See <see cref="DnsQueryType.A"/> and <see cref="DnsQueryType.AAAA"/>.
    /// </summary>
    public sealed class DnsIpAddressResource : IDnsResource, IEquatable<DnsIpAddressResource>
    {
        /// <summary>
        /// The Internet Protocol address.
        /// </summary>
        /// <value>
        /// May be an IPv4 or IPv6 address, depending on the record type.
        /// </value>
        public IPAddress IPAddress { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsIpAddressResource other) => IPAddress.Equals(other.IPAddress);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsIpAddressResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(IPAddress);

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset, int length) => IPAddress = new IPAddress(DnsByteExtensions.ReadBytes(bytes, length, ref offset));

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return IPAddress.GetAddressBytes();
        }
    }
}
