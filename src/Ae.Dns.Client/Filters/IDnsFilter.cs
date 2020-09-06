using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    public interface IDnsFilter
    {
        public bool IsPermitted(DnsHeader query);
    }
}
