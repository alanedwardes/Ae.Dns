namespace Ae.DnsResolver.Protocol
{
    public class DnsHeader
    {
        public ushort Id { get; set; }
        public ushort Header { get; set; }
        public short Qdcount { get; set; }
        public short Ancount { get; set; }
        public short Nscount { get; set; }
        public short Arcount { get; set; }
        public string[] Labels { get; set; }
        public DnsQueryType Qtype { get; set; }
        public DnsQueryClass Qclass { get; set; }

        public bool Qr
        {
            get
            {
                return false;
            }
            set
            {
                return;
            }
        }

        public override string ToString() => $"Id: {Id}, Domain: {string.Join(".", Labels)}, type: {Qtype}, class: {Qclass}";
    }
}
