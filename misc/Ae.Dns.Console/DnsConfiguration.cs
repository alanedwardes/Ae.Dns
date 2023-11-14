using System;

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
        public string? DhcpdLeasesFile { get; set; }
        public string? DhcpdConfigFile { get; set; }
        public string? DhcpdLeasesHostnameSuffix { get; set; }
        public DnsInfluxDbConfiguration? InfluxDbMetrics { get; set; }
    }
}
