using Ae.Dns.Protocol.Zone;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Provides methods to convert to and from the DNS zone file format.
    /// </summary>
    public interface IDnsZoneConverter
    {
        /// <summary>
        /// Write to a zone file string.
        /// </summary>
        /// <returns></returns>
        public string ToZone(IDnsZone zone);
        /// <summary>
        /// Read from a zone file string.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="input"></param>
        public void FromZone(IDnsZone zone, string input);
    }
}
