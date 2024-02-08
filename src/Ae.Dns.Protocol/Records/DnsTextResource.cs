using Ae.Dns.Protocol.Zone;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS text resource containing a string.
    /// </summary>
    public sealed class DnsTextResource : DnsStringResource
    {
        /// <inheritdoc/>
        protected override bool CanUseCompression => false;

        /// <inheritdoc/>
        public override string ToZone(IDnsZone zone)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override void FromZone(IDnsZone zone, string input)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override string ToString() => Entries;
    }
}
