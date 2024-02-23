using System;
using System.Collections.Generic;

namespace Ae.Dns.Console
{
    public sealed class DnsConfiguration
    {
        public Uri[] HttpsUpstreams { get; set; } = new Uri[0];
        public string[] UdpUpstreams { get; set; } = new string[0];
        public Uri[] RemoteBlocklists { get; set; } = new Uri[0];
        public string[] AllowlistedDomains { get; set; } = new string[0];
        public string[] DisallowedDomainSuffixes { get; set; } = new string[0];
        public string[] DisallowedQueryTypes { get; set; } = new string[0];
        public string[] HostFiles { get; set; } = new string[0];
        public string? UpdateZoneName { get; set; }
        public Dictionary<string, string[]> ClientGroups { get; set; } = new Dictionary<string, string[]>();
    }
}
