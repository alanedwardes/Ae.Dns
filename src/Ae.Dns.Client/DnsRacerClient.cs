using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client consisting of multiple <see cref="IDnsClient"/> instances.
    /// At query time, two clients are picked at random, and the DNS requests race against each other.
    /// The first non-exceptional result is used, the slower result is discarded.
    /// </summary>
    public sealed class DnsRacerClient : IDnsClient
    {
        private readonly IReadOnlyCollection<IDnsClient> _dnsClients;
        private readonly ILogger<DnsRacerClient> _logger;

        /// <summary>
        /// Create a new recer DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="dnsClients">The enumerable of DNS clients to use.</param>
        public DnsRacerClient(ILogger<DnsRacerClient> logger, IEnumerable<IDnsClient> dnsClients)
        {
            _logger = logger;
            _dnsClients = dnsClients.ToList();
            
            if (_dnsClients.Count < 2)
            {
                throw new InvalidOperationException("Must use at least two DNS clients");
            }
        }

        /// <summary>
        /// Create a new racer DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="dnsClients">The enumerable of DNS clients to use.</param>
        public DnsRacerClient(IEnumerable<IDnsClient> dnsClients) : this(NullLogger<DnsRacerClient>.Instance, dnsClients) { }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            // Randomise the clients
            var randomisedClients = _dnsClients.OrderBy(x => Guid.NewGuid()).ToArray();

            // Pick the first two
            var clientA = randomisedClients[0];
            var clientB = randomisedClients[1];

            // Start two queries in parallel
            var queryTaskA = clientA.Query(query, token);
            var queryTaskB = clientB.Query(query, token);

            // Identify the client from the task, used for logging
            IDnsClient GetClientFromTask(Task task) => task == queryTaskA ? clientA : clientB;

            // Select a winner
            var winningTask = await Task.WhenAny(queryTaskA, queryTaskB);
            var otherTask = winningTask == queryTaskA ? queryTaskB : queryTaskA;
            if (winningTask.IsFaulted && !otherTask.IsFaulted)
            {
                _logger.LogWarning("Task for client {FailedClient} faulted in {ElapsedMilliseconds} for query {Query}, swapping with result from client {SuccessfulClient}", GetClientFromTask(winningTask), query, sw.ElapsedMilliseconds, GetClientFromTask(otherTask));
                winningTask = otherTask;
            }

            // Handle the case where both failed
            if (winningTask.IsFaulted)
            {
                _logger.LogError("Both tasks using {ClientA} and {ClientB} faulted for query {Query} in {ElapsedMilliseconds}ms", clientA, clientB, query, sw.ElapsedMilliseconds);
            }

            // Log the winner
            var result = await winningTask;
            _logger.LogInformation("Winning client was {WinningClient} for query {Query} in {ElapsedMilliseconds}ms", GetClientFromTask(winningTask), query, sw.ElapsedMilliseconds);
            return result;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
