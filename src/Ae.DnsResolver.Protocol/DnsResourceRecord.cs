using Ae.DnsResolver.Protocol.Enums;
using System;

namespace Ae.DnsResolver.Protocol
{
    public class DnsResourceRecord
    {
        internal string[] Name { get; set; }
        public DnsQueryType Type { get; set; }
        public DnsQueryClass Class { get; set; }
        internal uint Ttl { get; set; }
        internal int DataOffset { get; set; }
        internal int DataLength { get; set; }

        public TimeSpan TimeToLive
        {
            get => TimeSpan.FromSeconds(Ttl);
            set => Ttl = (uint)value.TotalSeconds;
        }

        public string Host
        {
            get => string.Join(".", Name);
            set => Name = value.Split(".");
        }

        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {Ttl}";
    }
}
