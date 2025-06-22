using System;
using System.Collections.Generic;

namespace Ae.Dns.Console
{
    public sealed class DnsConfiguration
    {
        public Uri[] HttpsUpstreams { get; set; } = [];
        public string[] UdpUpstreams { get; set; } = [];
        public Uri[] RemoteBlocklists { get; set; } = [];
        public string[] AllowlistedDomains { get; set; } = [];
        public string[] DisallowedDomainSuffixes { get; set; } = [];
        public string[] DisallowedQueryTypes { get; set; } = [];
        public string[] HostFiles { get; set; } = [];
        public string? UpdateZoneName { get; set; }
        public Dictionary<string, string[]> ClientGroups { get; set; } = new Dictionary<string, string[]>();
        public DnsZoneConfiguration[] Zones { get; set; } = [];
    }

    public sealed class DnsZoneConfiguration
    {
        public required string File { get; set; }
        public bool AllowUpdate { get; set; }
        public bool AllowQuery { get; set; }
        public string[] Primaries { get; set; } = [];
        public string[] Secondaries { get; set; } = [];
    }
}
