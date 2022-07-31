using System.Collections.Generic;
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

        public static DnsRootServer A = new DnsRootServer('A', "198.41.0.4", "2001:503:ba3e::2:30", "Verisign");
        public static DnsRootServer B = new DnsRootServer('B', "199.9.14.201", "2001:500:200::b", "USC-ISI");
        public static DnsRootServer C = new DnsRootServer('C', "192.33.4.12", "2001:500:2::c", "Cogent Communications");
        public static DnsRootServer D = new DnsRootServer('D', "199.7.91.13", "2001:500:2d::d", "University of Maryland");
        public static DnsRootServer E = new DnsRootServer('E', "192.203.230.10", "2001:500:a8::e", "NASA Ames Research Center");
        public static DnsRootServer F = new DnsRootServer('F', "192.5.5.241", "2001:500:2f::f", "Internet Systems Consortium");
        public static DnsRootServer G = new DnsRootServer('G', "192.112.36.4", "2001:500:12::d0d", "Defense Information Systems Agency");
        public static DnsRootServer H = new DnsRootServer('H', "198.97.190.53", "2001:500:1::53", "U.S. Army Research Lab");
        public static DnsRootServer I = new DnsRootServer('I', "192.36.148.17", "2001:7fe::53", "Netnod");
        public static DnsRootServer J = new DnsRootServer('J', "192.58.128.30", "2001:503:c27::2:30", "Verisign");
        public static DnsRootServer K = new DnsRootServer('K', "193.0.14.129", "2001:7fd::1", "RIPE NCC");
        public static DnsRootServer L = new DnsRootServer('L', "199.7.83.42", "2001:500:9f::42", "ICANN");
        public static DnsRootServer M = new DnsRootServer('M', "202.12.27.33", "2001:dc3::35", "WIDE Project");

        public static IReadOnlyList<DnsRootServer> All => new[] { A, B, C, D, E, F, G, H, I, J, K, L, M };
    }
}

