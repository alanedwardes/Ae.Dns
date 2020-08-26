using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsAnswer
    {
        public DnsHeader Header { get; set; }

        public IList<DnsResourceRecord> Answers { get; set; } = new List<DnsResourceRecord>();

        public override string ToString() => $"RESPONSE: {Header}";
    }
}
