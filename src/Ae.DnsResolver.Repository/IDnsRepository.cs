using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public interface IDnsRepository
    {
        Task<byte[]> Resolve(byte[] query);
    }
}
