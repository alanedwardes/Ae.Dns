using Ae.Dns.Protocol;
using System.Threading.Tasks;

namespace Ae.Dns.Repository
{
    public interface IDnsRepository
    {
        Task<DnsAnswer> Resolve(DnsHeader query);
    }
}
