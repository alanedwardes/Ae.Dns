namespace Ae.Dns.Protocol.Records.ServiceBinding
{
    /// <summary>
    /// Service Binding (SVCB) Parameter Registry
    /// See https://www.ietf.org/archive/id/draft-ietf-dnsop-svcb-https-12.html#name-initial-contents
    /// </summary>
    public enum SvcParameter : ushort
    {
        /// <summary>
        /// Mandatory keys in this RR
        /// </summary>
        Mandatory = 0,
        /// <summary>
        /// Additional supported protocols
        /// </summary>
        Alpn = 1,
        /// <summary>
        /// No support for defualt protocol
        /// </summary>
        NoDefaultAlpn = 2,
        /// <summary>
        /// Port for alternative endpoint
        /// </summary>
        Port = 3,
        /// <summary>
        /// IPv4 address hints
        /// </summary>
        IPv4Hint = 4,
        /// <summary>
        /// RESERVED (will be used for ECH)
        /// </summary>
        Ech = 5,
        /// <summary>
        /// IPv6 address hints
        /// </summary>
        IPv6Hint = 6,
        /// <summary>
        /// DNS over HTTPS path template
        /// </summary>
        Dohpath = 7,
        /// <summary>
        /// Reserved ("Invalid key")
        /// </summary>
        Invalid = 65535
    }
}
