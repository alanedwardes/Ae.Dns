using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
{
    public interface IDnsServer : IDisposable
    {
        Task Listen(CancellationToken token);
    }
}
