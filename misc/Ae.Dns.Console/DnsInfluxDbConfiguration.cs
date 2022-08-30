using System;

namespace Ae.Dns.Console
{
    public sealed class DnsInfluxDbConfiguration
    {
        public Uri BaseUri { get; set; }
        public string Organization { get; set; }
        public string Bucket { get; set; }
        public string Token { get; set; }
    }
}
