using Ae.Dns.Protocol.Enums;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Extension methods for <see cref="IDnsResource"/>.
    /// </summary>
    public static class DnsResourceFactory
    {
        /// <summary>
        /// Create the appropriate <see cref="IDnsResource"/> for the specified <see cref="DnsQueryType"/>.
        /// </summary>
        /// <param name="recordType"></param>
        /// <returns></returns>
        public static IDnsResource CreateResource(DnsQueryType recordType)
        {
            return recordType switch
            {
                DnsQueryType.A => new DnsIpAddressResource(),
                DnsQueryType.AAAA => new DnsIpAddressResource(),
                DnsQueryType.TEXT => new DnsTextResource(),
                DnsQueryType.CNAME => new DnsDomainResource(),
                DnsQueryType.NS => new DnsDomainResource(),
                DnsQueryType.PTR => new DnsDomainResource(),
                DnsQueryType.SPF => new DnsTextResource(),
                DnsQueryType.SOA => new DnsSoaResource(),
                DnsQueryType.MX => new DnsMxResource(),
                DnsQueryType.OPT => new DnsOptResource(),
                DnsQueryType.HTTPS => new DnsServiceBindingResource(),
                DnsQueryType.SVCB => new DnsServiceBindingResource(),
                _ => new DnsUnknownResource(),
            };
        }
    }
}
