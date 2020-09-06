using Ae.Dns.Protocol;

namespace Ae.Dns.Server.Filters
{
    public interface IDnsFilter
    {
        public bool IsPermitted(DnsHeader query);
    }
}
