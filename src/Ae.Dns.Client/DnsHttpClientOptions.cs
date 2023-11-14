namespace Ae.Dns.Client
{
    /// <summary>
    /// Defines options pertaining to the DNS HTTP client.
    /// </summary>
    public sealed class DnsHttpClientOptions
    {
        /// <summary>
        /// The path to send DNS queries to, normally /dns-query
        /// </summary>
        public string Path { get; set; } = "/dns-query";
    }
}
