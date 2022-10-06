namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS text resource containing a string.
    /// </summary>
    public sealed class DnsTextResource : DnsStringResource
    {
        /// <inheritdoc/>
        public override string ToString() => '"' + string.Join("\", \"", Entries) + '"';
    }
}
