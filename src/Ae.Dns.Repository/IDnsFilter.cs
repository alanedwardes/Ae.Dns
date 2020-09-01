using Ae.Dns.Protocol;

namespace Ae.Dns.Repository
{
    public interface IDnsFilter
    {
        public bool IsPermitted(DnsHeader query);
    }
}
