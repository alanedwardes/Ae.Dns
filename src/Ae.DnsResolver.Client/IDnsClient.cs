using System.Threading.Tasks;

namespace Ae.DnsResolver.Client
{
    public interface IDnsClient
    {
        Task<byte[]> LookupRaw(byte[] raw);
    }
}
