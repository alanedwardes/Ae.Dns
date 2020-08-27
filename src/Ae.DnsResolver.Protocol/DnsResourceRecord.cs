using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Net;

namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsMxRecord
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }
    }

    public sealed class DnsSoaRecord
    {
        public string MName { get; set; }
        public string RName { get; set; }
        public uint Serial { get; set; }
        public int Refresh { get; set; }
        public int Retry { get; set; }
        public int Expire { get; set; }
        public uint Minimum { get; set; }
    }

    public sealed class DnsResourceRecord
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

        /// <summary>
        /// Set if the type of record is <see cref="DnsQueryType.A"/> or <see cref="DnsQueryType.AAAA"/>
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Set if the type of record is <see cref="DnsQueryType.CNAME"/> or <see cref="DnsQueryType.TEXT"/>.
        /// </summary>
        public string Text { get; set; }

        public DnsMxRecord MxRecord { get; set; }

        public DnsSoaRecord SoaRecord { get; set; }

        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {Ttl}";
    }
}
