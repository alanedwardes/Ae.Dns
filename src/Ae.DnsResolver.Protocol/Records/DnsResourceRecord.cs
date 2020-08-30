using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Protocol.Records
{
    public abstract class DnsResourceRecord
    {
        internal string[] Name { get; set; }
        public DnsQueryType Type { get; set; }
        public DnsQueryClass Class { get; set; }
        internal uint Ttl { get; set; }
        internal ushort DataLength { get; set; }

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

        protected abstract void ReadBytes(byte[] bytes, ref int offset);
        internal void ReadData(byte[] bytes, ref int offset)
        {
            var dataOffset = offset;

            ReadBytes(bytes, ref offset);

            var expectedOffset = dataOffset + DataLength;
            if (offset != expectedOffset)
            {
                var reader = GetType();
                throw new InvalidOperationException($"{reader.Name} did not read to offset {expectedOffset} (read to {offset})");
            }
        }
        protected abstract IEnumerable<IEnumerable<byte>> WriteBytes();
        internal IEnumerable<byte> WriteData() => WriteBytes().SelectMany(x => x);

        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {Ttl}";
    }
}
