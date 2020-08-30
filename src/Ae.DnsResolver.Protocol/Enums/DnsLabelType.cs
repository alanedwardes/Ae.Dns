namespace Ae.DnsResolver.Protocol.Enums
{
    /// <summary>
    /// DNS label type. See https://www.iana.org/assignments/dns-parameters/dns-parameters.xml#dns-parameters-10
    /// </summary>
    public enum DnsLabelType : byte
    {
        /// <summary>
        /// Normal label - lower 6 bits is the length of the label
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Compressed label - the lower 6 bits and the 8 bits from next octet form a pointer to the compression target.
        /// </summary>
        Compressed = 192
    }
}
