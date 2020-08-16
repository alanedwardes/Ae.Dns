using Ae.DnsResolver.Protocol;

namespace Ae.DnsResolver.Repository
{
    public interface IDnsFilter
    {
        public bool IsPermitted(DnsHeader query);
    }
}
