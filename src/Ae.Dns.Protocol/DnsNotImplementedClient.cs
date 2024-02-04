using Ae.Dns.Protocol.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// A client which only returns a <see cref="DnsResponseCode.NotImp"/> code.
    /// </summary>
    public sealed class DnsNotImplementedClient : IDnsClient
    {
        /// <summary>
        /// A static instance of this client which can be used to avoid allocating a new one.
        /// </summary>
        public static readonly IDnsClient Instance = new DnsNotImplementedClient();

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            return Task.FromResult(query.CreateErrorMessage(DnsResponseCode.NotImp, ToString()));
        }

        /// <inheritdoc/>
        public override string ToString() => nameof(DnsNotImplementedClient);
    }
}
