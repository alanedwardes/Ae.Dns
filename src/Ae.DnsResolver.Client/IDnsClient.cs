using Ae.DnsResolver.Protocol;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public interface IDnsClient
    {
        Task<DnsAnswer> Query(DnsHeader query);
    }
}
