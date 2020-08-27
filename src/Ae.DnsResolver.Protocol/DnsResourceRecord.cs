using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Net;

namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsMxRecord : DnsResourceRecord
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }
    }

    public sealed class DnsSoaRecord : DnsResourceRecord
    {
        public string MName { get; set; }
        public string RName { get; set; }
        public uint Serial { get; set; }
        public int Refresh { get; set; }
        public int Retry { get; set; }
        public int Expire { get; set; }
        public uint Minimum { get; set; }
    }

    public sealed class DnsIpAddressRecord : DnsResourceRecord
    {
        public IPAddress IPAddress { get; set; }
    }

    public sealed class DnsTextRecord : DnsResourceRecord
    {
        public string Text { get; set; }
    }

    public sealed class UnimplementedDnsResourceRecord : DnsResourceRecord
    {
    }

    public abstract class DnsResourceRecord
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
