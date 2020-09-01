using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsResourceRecord : IEquatable<DnsResourceRecord>, IByteArrayReader
    {
        internal string[] Name { get; set; }
        public DnsQueryType Type { get; set; }
        public DnsQueryClass Class { get; set; }
        public TimeSpan TimeToLive { get; set; }

        public string Host
        {
            get => string.Join('.', Name);
            set => Name = value.Split('.');
        }

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

        public IDnsResource Resource { get; set; }

        private IDnsResource CreateResourceRecord(DnsQueryType recordType)
        {
            return _recordTypeFactories.TryGetValue(recordType, out var factory) ? factory() : new UnknownDnsResource();
        }

        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {TimeToLive}";

        public bool Equals(DnsResourceRecord other) => Host == other.Host && Type == other.Type && Class == other.Class && TimeToLive == other.TimeToLive && Resource.Equals(other.Resource);

        public override bool Equals(object obj) => obj is DnsResourceRecord record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(Name, Type, Class, TimeToLive, Host, Resource);

        public void ReadBytes(byte[] bytes, ref int offset)
        {
            Name = bytes.ReadString(ref offset);
            Type = (DnsQueryType)bytes.ReadUInt16(ref offset);
            Class = (DnsQueryClass)bytes.ReadUInt16(ref offset);
            TimeToLive = TimeSpan.FromSeconds(bytes.ReadUInt32(ref offset));
            Resource = CreateResourceRecord(Type);
            var dataLength = bytes.ReadUInt16(ref offset);
            FromBytesKnownLength(Resource, bytes, ref offset, dataLength);
        }

        private void FromBytesKnownLength(IDnsResource resource, byte[] bytes, ref int offset, int length)
        {
            var expectedOffset = offset + length;
            resource.ReadBytes(bytes, ref offset, length);
            if (offset != expectedOffset)
            {
                throw new InvalidOperationException($"{resource.GetType().Name}.{nameof(IDnsResource.ReadBytes)} did not read to offset {expectedOffset} (read to {offset})");
            }
        }

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Name.ToBytes();
            yield return Type.ToBytes();
            yield return Class.ToBytes();
            yield return ((uint)TimeToLive.TotalSeconds).ToBytes();
            var data = Resource.WriteBytes().SelectMany(x => x).ToArray();
            yield return ((ushort)data.Length).ToBytes();
            yield return data;
        }
    }
}
