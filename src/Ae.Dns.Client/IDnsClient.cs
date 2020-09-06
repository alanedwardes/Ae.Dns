using Ae.Dns.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public interface IDnsClient : IDisposable
    {
        Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default);
    }
}
