using Ae.DnsResolver.Protocol;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public interface IDnsRepository
    {
        Task<DnsAnswer> Resolve(DnsHeader query);
    }
}
