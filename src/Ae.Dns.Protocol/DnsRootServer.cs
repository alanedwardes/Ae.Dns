using System.Net;

namespace Ae.Dns.Protocol
{
    public sealed class DnsRootServer
    {
        public DnsRootServer(char letter, string ipv4, string ipv6, string op)
        {
            Letter = char.ToUpperInvariant(letter);
            Ipv4Address = IPAddress.Parse(ipv4);
            Ipv6Address = IPAddress.Parse(ipv6);
            Operator = op;
        }

        public char Letter { get; }
        public IPAddress Ipv4Address { get; }
        public IPAddress Ipv6Address { get; }
        public string Hostname => $"{char.ToLowerInvariant(Letter)}.root-servers.net";
        public string Operator { get; }

        public override string ToString() => $"{Hostname} ({Operator})";
    }
}

