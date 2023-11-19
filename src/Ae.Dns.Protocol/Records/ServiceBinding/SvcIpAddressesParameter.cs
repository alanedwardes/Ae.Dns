using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Ae.Dns.Protocol.Records.ServiceBinding
{
    /// <summary>
    /// See https://www.ietf.org/archive/id/draft-ietf-dnsop-svcb-https-12.html#name-ipv4hint-and-ipv6hint
    /// </summary>
    public sealed class SvcIpAddressesParameter : IDnsResource, IEquatable<SvcIpAddressesParameter>
    {
        /// <summary>
        /// Constructs an IP address parameter using IPv4 or IPv6 addresses.
        /// </summary>
        public SvcIpAddressesParameter(AddressFamily addressFamily) => AddressFamily = addressFamily;

        /// <summary>
        /// A list of IPv4 or IPv6 addresses depending on the parameter type.
        /// All addresses will be IPv4 or IPv6, they will not be mixed.
        /// </summary>
        public IPAddress[] Entries { get; set; } = Array.Empty<IPAddress>();

        /// <summary>
        /// The address family described by this record, either IPv4 or IPv6.
        /// </summary>
        public AddressFamily AddressFamily { get; }

        /// <inheritdoc/>
        public bool Equals(SvcIpAddressesParameter? other)
        {
            if (other == null)
            {
                return false;
            }

            return Entries.SequenceEqual(other.Entries);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SvcIpAddressesParameter record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Entries);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            var addressSize = AddressFamily switch
            {
                AddressFamily.InterNetworkV6 => 16,
                AddressFamily.InterNetwork => 4,
                _ => throw new NotSupportedException($"Address family {AddressFamily} not supported")
            };

            Entries = new IPAddress[length / addressSize];

            for (var i = 0; i < Entries.Length; i++)
            {
                var raw = DnsByteExtensions.ReadBytes(bytes, addressSize, ref offset);
                Entries[i] = new IPAddress(raw.ToArray());
            }
        }

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            foreach (var entry in Entries)
            {
                Span<byte> address = entry.GetAddressBytes();

                address.CopyTo(bytes.Slice(offset).Span);
                offset += address.Length;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => string.Join(",", Entries.Select(x => x.ToString()));
    }
}
