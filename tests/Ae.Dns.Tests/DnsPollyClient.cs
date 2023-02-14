using Ae.Dns.Protocol;
using Polly;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Polly
{
    /// <inheritdoc/>
    public sealed class DnsPollyClient : IDnsClient
    {
        private readonly IDnsClient _dnsClient;
        private readonly AsyncPolicy<DnsMessage> _policy;

        /// <summary>
        /// Construct a polly retry client using the specified retry policy.
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="policy"></param>
        public DnsPollyClient(IDnsClient dnsClient, AsyncPolicy<DnsMessage> policy)
        {
            _dnsClient = dnsClient;
            _policy = policy;
        }

        /// <inheritdoc/>
        public void Dispose() => _dnsClient.Dispose();

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            return await _policy.ExecuteAsync(innerToken => _dnsClient.Query(query, innerToken), token);
        }
    }
}
