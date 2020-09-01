using Ae.Dns.Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public interface IDnsClient
    {
        Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default);
    }
}
