using Ae.DnsResolver.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsAnswer : IEquatable<DnsAnswer>
    {
        public DnsHeader Header { get; set; }

        public IList<DnsResourceRecord> Answers { get; set; } = new List<DnsResourceRecord>();

        public bool Equals(DnsAnswer other) => Header.Equals(other.Header) && Answers.SequenceEqual(other.Answers);

        public override int GetHashCode()
        {
            return HashCode.Combine(Header, Answers);
        }

        public override string ToString() => $"RESPONSE: {Header}";
    }
}
