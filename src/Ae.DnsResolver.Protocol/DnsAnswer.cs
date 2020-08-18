namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsAnswer
    {
        public DnsHeader Header;

        public DnsResourceRecord[] Answers;

        public override string ToString() => $"RESPONSE: {Header}";
    }
}
