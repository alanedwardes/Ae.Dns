using Ae.Dns.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    public abstract class DnsResourceRecord : IEquatable<DnsResourceRecord>
    {
        internal string[] Name { get; set; }
        public DnsQueryType Type { get; set; }
        public DnsQueryClass Class { get; set; }
        internal uint Ttl { get; set; }

        public TimeSpan TimeToLive
        {
            get => TimeSpan.FromSeconds(Ttl);
            set => Ttl = (uint)value.TotalSeconds;
        }

        public string Host
        {
            get => string.Join('.', Name);
            set => Name = value.Split('.');
        }

        protected abstract void ReadBytes(byte[] bytes, ref int offset, int expectedLength);
        internal void ReadData(byte[] bytes, ref int offset, int expectedLength)
        {
            var dataOffset = offset;

            ReadBytes(bytes, ref offset, expectedLength);

            var expectedOffset = dataOffset + expectedLength;
            if (offset != expectedOffset)
            {
                var reader = GetType();
                throw new InvalidOperationException($"{reader.Name} did not read to offset {expectedOffset} (read to {offset})");
            }
        }
        protected abstract IEnumerable<IEnumerable<byte>> WriteBytes();
        internal IEnumerable<byte> WriteData() => WriteBytes().SelectMany(x => x);

        private static IReadOnlyDictionary<DnsQueryType, Func<DnsResourceRecord>> _recordTypeFactories = new Dictionary<DnsQueryType, Func<DnsResourceRecord>>
        {
            { DnsQueryType.A, () => new DnsIpAddressRecord() },
            { DnsQueryType.AAAA, () => new DnsIpAddressRecord() },
            { DnsQueryType.TEXT, () => new DnsTextRecord() },
            { DnsQueryType.CNAME, () => new DnsTextRecord() },
            { DnsQueryType.NS, () => new DnsTextRecord() },
            { DnsQueryType.PTR, () => new DnsTextRecord() },
            { DnsQueryType.SPF, () => new DnsTextRecord() },
            { DnsQueryType.SOA, () => new DnsSoaRecord() },
            { DnsQueryType.MX, () => new DnsMxRecord() }
        };

        internal static DnsResourceRecord CreateResourceRecord(DnsQueryType recordType)
        {
            return _recordTypeFactories.TryGetValue(recordType, out var factory) ? factory() : new UnimplementedDnsResourceRecord();
        }

        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {Ttl}";

        public bool Equals(DnsResourceRecord other)
        {
            return Host == other.Host &&
                   Type == other.Type &&
                   Class == other.Class &&
                   TimeToLive == other.TimeToLive;
        }

        public override bool Equals(object obj) => obj is DnsResourceRecord record ? Equals(record) : base.Equals(obj);
    }
}
