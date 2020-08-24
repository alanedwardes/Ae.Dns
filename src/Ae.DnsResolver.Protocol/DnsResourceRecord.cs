using Ae.DnsResolver.Protocol.Enums;
using System;

namespace Ae.DnsResolver.Protocol
{
    public class DnsResourceRecord
    {
        public string[] Name;
        public DnsQueryType Type;
        public DnsQueryClass Class;
        public TimeSpan Ttl;
        public int DataOffset;
        public int DataLength;

        public override string ToString() => $"Name: {string.Join(", ", Name)} Type: {Type} Class: {Class} TTL: {Ttl}";
    }
}
