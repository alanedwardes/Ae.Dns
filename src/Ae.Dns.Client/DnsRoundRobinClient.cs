﻿using Ae.Dns.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client consisting of multiple <see cref="IDnsClient"/> instances which are picked at random.
    /// </summary>
    [Obsolete("This client does not do round robin. Please use DnsRandomClient")]
    public sealed class DnsRoundRobinClient : IDnsClient
    {
        private readonly IReadOnlyCollection<IDnsClient> _dnsClients;

        /// <summary>
        /// Create a new round-robin DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="dnsClients">The enumerable of DNS clients to use.</param>
        [Obsolete("This client does not do round robin. Please use DnsRandomClient")]
        public DnsRoundRobinClient(IEnumerable<IDnsClient> dnsClients) => _dnsClients = dnsClients.ToList();

        /// <summary>
        /// Create a new round-robin DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="dnsClients">The array of DNS clients to use.</param>
        [Obsolete("This client does not do round robin. Please use DnsRandomClient")]
        public DnsRoundRobinClient(params IDnsClient[] dnsClients) => _dnsClients = dnsClients;

        private IDnsClient GetRandomClient() => _dnsClients.OrderBy(x => Guid.NewGuid()).First();

        /// <inheritdoc/>
        [Obsolete("This client does not do round robin. Please use DnsRandomClient")]
        public Task<DnsMessage> Query(DnsMessage query, CancellationToken token) => GetRandomClient().Query(query, token);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
