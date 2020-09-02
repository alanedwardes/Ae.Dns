using Ae.Dns.Protocol;

namespace Ae.Dns.Server
{
    public interface IDnsFilter
    {
        public bool IsPermitted(DnsHeader query);
    }
}
