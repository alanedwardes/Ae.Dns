namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS text resource containing a domain name.
    /// </summary>
    public sealed class DnsDomainResource : DnsStringResource
    {
        /// <inheritdoc/>
        protected override bool CanUseCompression => true;

        /// <summary>
        /// Get the value of this entry as a domain name.
        /// </summary>
        public string Domain => Entries;

        /// <inheritdoc/>
        public override string ToString() => Domain;
    }
}
