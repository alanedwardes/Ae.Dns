using Ae.Dns.Client.Internal;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        private readonly DnsRacerClientOptions _options;

        /// <summary>
        /// Create a new racer DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="options">The options for this racer client.</param>
        /// <param name="dnsClients">The enumerable of DNS clients to use.</param>
        [ActivatorUtilitiesConstructor]
        public DnsRacerClient(ILogger<DnsRacerClient> logger, IOptions<DnsRacerClientOptions> options, IEnumerable<IDnsClient> dnsClients)
        {
            _logger = logger;
            _options = options.Value;
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
        public DnsRacerClient(params IDnsClient[] dnsClients)
            : this(NullLogger<DnsRacerClient>.Instance, Options.Create(new DnsRacerClientOptions()), dnsClients)
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            // Randomise the clients
            var randomisedClients = _dnsClients.OrderBy(x => Guid.NewGuid()).Take(_options.RandomClientQueries).ToArray();

            // Start the query tasks (and create a lookup from query to client)
            var queries = randomisedClients.ToDictionary(client => client.Query(query, token), client => client);

            // Select a winning task
            var winningTask = await TaskRacer.RaceTasks(queries.Select(x => x.Key));

            // If tasks faulted, log the reason
            var faultedTasks = queries.Keys.Where(x => x.IsFaulted).ToArray();
            if (faultedTasks.Any())
            {
                var faultedClients = string.Join(", ", faultedTasks.Select(x => queries[x]));
                if (winningTask.IsFaulted)
                {
                    _logger.LogError("All tasks using {FaultedClients} failed for query {Query} in {ElapsedMilliseconds}ms", faultedClients, query, sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Tasks for clients {FaultedClients} failed for query {Query}, swapped with result for {SuccessfulClient} in {ElapsedMilliseconds}ms", faultedClients, query, queries[winningTask], sw.ElapsedMilliseconds);
                }
            }

            // Await the winning task (if it's faulted, it will throw at this point)
            var winningAnswer = await winningTask;
            _logger.LogInformation("Winning client was {WinningClient} for query {Query} in {ElapsedMilliseconds}ms", queries[winningTask], query, sw.ElapsedMilliseconds);
            return winningAnswer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
