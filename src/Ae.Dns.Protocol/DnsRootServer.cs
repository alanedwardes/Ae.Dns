using System.Collections.Generic;
using System.Net;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// A structure describing the DNS root servers.
    /// </summary>
    public sealed class DnsRootServer
    {
        /// <summary>
        /// Construct a new DNS root server.
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="ipv4"></param>
        /// <param name="ipv6"></param>
        /// <param name="op"></param>
        public DnsRootServer(char letter, string ipv4, string ipv6, string op)
        {
            Letter = char.ToUpperInvariant(letter);
            Ipv4Address = IPAddress.Parse(ipv4);
            Ipv6Address = IPAddress.Parse(ipv6);
            Operator = op;
        }

        /// <summary>
        /// The single letter identifier of this root server.
        /// </summary>
        public char Letter { get; }
        /// <summary>
        /// The IPv4 address of this root server.
        /// </summary>
        public IPAddress Ipv4Address { get; }
        /// <summary>
        /// The IPv6 address of this root server.
        /// </summary>
        public IPAddress Ipv6Address { get; }
        /// <summary>
        /// The DNS name of this root server.
        /// </summary>
        public string Hostname => $"{char.ToLowerInvariant(Letter)}.root-servers.net";
        /// <summary>
        /// The operator of the root server.
        /// </summary>
        public string Operator { get; }
        /// <inheritdoc/>
        public override string ToString() => $"{Hostname} ({Operator})";
        /// <summary>
        /// DNS root server A
        /// </summary>
        public static readonly DnsRootServer A = new DnsRootServer('A', "198.41.0.4", "2001:503:ba3e::2:30", "Verisign");
        /// <summary>
        /// DNS root server B
        /// </summary>
        public static readonly DnsRootServer B = new DnsRootServer('B', "199.9.14.201", "2001:500:200::b", "USC-ISI");
        /// <summary>
        /// DNS root server C
        /// </summary>
        public static readonly DnsRootServer C = new DnsRootServer('C', "192.33.4.12", "2001:500:2::c", "Cogent Communications");
        /// <summary>
        /// DNS root server D
        /// </summary>
        public static readonly DnsRootServer D = new DnsRootServer('D', "199.7.91.13", "2001:500:2d::d", "University of Maryland");
        /// <summary>
        /// DNS root server E
        /// </summary>
        public static readonly DnsRootServer E = new DnsRootServer('E', "192.203.230.10", "2001:500:a8::e", "NASA Ames Research Center");
        /// <summary>
        /// DNS root server F
        /// </summary>
        public static readonly DnsRootServer F = new DnsRootServer('F', "192.5.5.241", "2001:500:2f::f", "Internet Systems Consortium");
        /// <summary>
        /// DNS root server G
        /// </summary>
        public static readonly DnsRootServer G = new DnsRootServer('G', "192.112.36.4", "2001:500:12::d0d", "Defense Information Systems Agency");
        /// <summary>
        /// DNS root server H
        /// </summary>
        public static readonly DnsRootServer H = new DnsRootServer('H', "198.97.190.53", "2001:500:1::53", "U.S. Army Research Lab");
        /// <summary>
        /// DNS root server I
        /// </summary>
        public static readonly DnsRootServer I = new DnsRootServer('I', "192.36.148.17", "2001:7fe::53", "Netnod");
        /// <summary>
        /// DNS root server J
        /// </summary>
        public static readonly DnsRootServer J = new DnsRootServer('J', "192.58.128.30", "2001:503:c27::2:30", "Verisign");
        /// <summary>
        /// DNS root server K
        /// </summary>
        public static readonly DnsRootServer K = new DnsRootServer('K', "193.0.14.129", "2001:7fd::1", "RIPE NCC");
        /// <summary>
        /// DNS root server L
        /// </summary>
        public static readonly DnsRootServer L = new DnsRootServer('L', "199.7.83.42", "2001:500:9f::42", "ICANN");
        /// <summary>
        /// DNS root server M
        /// </summary>
        public static readonly DnsRootServer M = new DnsRootServer('M', "202.12.27.33", "2001:dc3::35", "WIDE Project");
        /// <summary>
        /// An enumerable containing all DNS root servers.
        /// </summary>
        public static IEnumerable<DnsRootServer> All => new[] { A, B, C, D, E, F, G, H, I, J, K, L, M };
    }
}

