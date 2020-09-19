using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    public interface IDnsServer : IDisposable
    {
        Task Listen(CancellationToken token = default);
    }
}
