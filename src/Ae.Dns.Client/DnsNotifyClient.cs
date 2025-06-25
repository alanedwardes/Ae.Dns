using System;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A DNS client that recieves notifications from a DNS server.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsNotifyClient : IDnsClient
    {
        private readonly Func<DnsMessage, Task> _notify;

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsNotifyClient"/> class.
        /// </summary>
        /// <param name="notify"></param>
        public DnsNotifyClient(Func<DnsMessage, Task> notify = null)
        {
            _notify = notify;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            query.EnsureOperationCode(DnsOperationCode.NOTIFY);

            await _notify?.Invoke(query);

            return DnsMessageExtensions.CreateAnswerMessage(query, DnsResponseCode.NoError, ToString());
        }
    }
}
