using Ae.Dns.Protocol.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// A simple DNS message processor which uses the query operation code to delegate to other <see cref="IDnsClient"/> implementations.
    /// </summary>
    public sealed class DnsOperationRouter : IDnsClient
    {
        private readonly IReadOnlyDictionary<DnsOperationCode, IDnsClient> _routes;

        /// <summary>
        /// Construct a new <see cref="DnsOperationRouter"/> using the specified <see cref="IDnsClient"/> implementations for querying, and updating (if applicable).
        /// If null is supplied (for example for the update client), a <see cref="DnsResponseCode.NotImp"/> answer will be returned.
        /// </summary>
        /// <param name="routes"></param>
        public DnsOperationRouter(IReadOnlyDictionary<DnsOperationCode, IDnsClient> routes)
        {
            _routes = routes;
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            _routes.TryGetValue(query.Header.OperationCode, out IDnsClient? client);

            if (client == null)
            {
                return query.CreateErrorMessage(DnsResponseCode.NotImp, ToString());
            }

            return await client.Query(query, token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsOperationRouter)}({string.Join(", ", _routes.Keys)})";
    }
}
