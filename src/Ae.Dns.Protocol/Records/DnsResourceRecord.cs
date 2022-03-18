using Ae.Dns.Protocol.Enums;
using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents metadata around a DNS resource record returned by a DNS server.
    /// </summary>
    public sealed class DnsResourceRecord : IEquatable<DnsResourceRecord>, IDnsByteArrayReader, IDnsByteArrayWriter
    {
        /// <summary>
        /// The type of DNS query.
        /// </summary>
        public DnsQueryType Type { get; set; }
        /// <summary>
        /// The class of DNS query.
        /// </summary>
        public DnsQueryClass Class { get; set; }
        /// <summary>
        /// The time to live entry for this record.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// The host name associated with this record.
        /// </summary>
        public string Host
        {
            get => string.Join(".", Name);
            set => Name = value.Split('.');
        }

        private string[] Name { get; set; }

        private static IReadOnlyDictionary<DnsQueryType, Func<IDnsResource>> _recordTypeFactories = new Dictionary<DnsQueryType, Func<IDnsResource>>
        {
            { DnsQueryType.A, () => new DnsIpAddressResource() },
            { DnsQueryType.AAAA, () => new DnsIpAddressResource() },
            { DnsQueryType.TEXT, () => new DnsTextResource() },
            { DnsQueryType.CNAME, () => new DnsTextResource() },
            { DnsQueryType.NS, () => new DnsTextResource() },
            { DnsQueryType.PTR, () => new DnsTextResource() },
            { DnsQueryType.SPF, () => new DnsTextResource() },
            { DnsQueryType.SOA, () => new DnsSoaResource() },
            { DnsQueryType.MX, () => new DnsMxResource() }
        };

        /// <summary>
        /// The value of this DNS record, which should be
        /// cast to the appropriate resource record type
        /// class depending on the <see cref="Type"/>.
        /// </summary>
        public IDnsResource Resource { get; set; }

        private IDnsResource CreateResourceRecord(DnsQueryType recordType)
        {
            return _recordTypeFactories.TryGetValue(recordType, out var factory) ? factory() : new DnsUnknownResource();
        }

        /// <inheritdoc/>
        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {TimeToLive} Resource: {Resource}";

        /// <inheritdoc/>
        public bool Equals(DnsResourceRecord other) => Host == other.Host && Type == other.Type && Class == other.Class && TimeToLive == other.TimeToLive && Resource.Equals(other.Resource);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsResourceRecord record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Name, Type, Class, TimeToLive, Host, Resource);

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset)
        {
            Name = DnsByteExtensions.ReadString(bytes, ref offset);
            Type = (DnsQueryType)DnsByteExtensions.ReadUInt16(bytes, ref offset);
            Class = (DnsQueryClass)DnsByteExtensions.ReadUInt16(bytes, ref offset);
            TimeToLive = TimeSpan.FromSeconds(DnsByteExtensions.ReadUInt32(bytes, ref offset));
            Resource = CreateResourceRecord(Type);
            var dataLength = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            FromBytesKnownLength(Resource, bytes, ref offset, dataLength);
        }

        private static void FromBytesKnownLength(IDnsResource resource, byte[] bytes, ref int offset, int length)
        {
            var expectedOffset = offset + length;
            resource.ReadBytes(bytes, ref offset, length);
            if (offset != expectedOffset)
            {
                throw new InvalidOperationException($"{resource.GetType().Name}.{nameof(IDnsResource.ReadBytes)} did not read to offset {expectedOffset} (read to {offset})");
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return DnsByteExtensions.ToBytes(Name);
            yield return DnsByteExtensions.ToBytes(Type);
            yield return DnsByteExtensions.ToBytes(Class);
            yield return DnsByteExtensions.ToBytes((uint)TimeToLive.TotalSeconds);
            var data = DnsByteExtensions.ToBytes(Resource);
            yield return DnsByteExtensions.ToBytes((ushort)data.Length);
            yield return data;
        }
    }
}
