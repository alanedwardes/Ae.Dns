using Ae.Dns.Client.Internal;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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
    public sealed class DnsGroupRacerClient : IDnsClient
    {
        private readonly ILogger<DnsGroupRacerClient> _logger;
        private readonly DnsGroupRacerClientOptions _options;

        /// <summary>
        /// Create a new racer DNS client using the specified <see cref="IDnsClient"/> instances to delegate to.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="options">The options for this racer client.</param>
        [ActivatorUtilitiesConstructor]
        public DnsGroupRacerClient(ILogger<DnsGroupRacerClient> logger, IOptions<DnsGroupRacerClientOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            // Pick the specified number of random groups
            var randomisedGroups = _options.DnsClientGroups.Where(x => x.Value.Count > 0).OrderBy(x => Guid.NewGuid()).Take(_options.RandomGroupQueries);

            // Randomise a client from each group
            var randomisedClients = randomisedGroups.ToDictionary(x => x.Value.OrderBy(y => Guid.NewGuid()).First(), x => x.Key);

            // Start the query tasks (and create a lookup from query to client)
            var queries = randomisedClients.Keys.ToDictionary(client => QueryWrapped(client, query, token), client => client);

            // Select a winning task
            var winningTask = await TaskRacer.RaceTasks(queries.Select(x => x.Key));

            // If tasks faulted, log the reason
            var faultedTasks = queries.Keys.Where(x => x.IsFaulted).ToArray();
            if (faultedTasks.Any())
            {
                var faultedClients = faultedTasks.Select(x => queries[x]);
                var faultedGroups = faultedClients.Select(x => randomisedClients[x]);

                var faultedClientsString = string.Join(", ", faultedClients);
                var faultedGroupsString = string.Join(", ", faultedGroups);

                if (winningTask.IsFaulted)
                {
                    _logger.LogError("All tasks using {FaultedClients} from groups {FaultedGroups} failed for query {Query} in {ElapsedMilliseconds}ms", faultedClientsString, faultedGroupsString, query, sw.ElapsedMilliseconds);
                    return DnsQueryFactory.CreateErrorResponse(query);
                }
                else
                {
                    _logger.LogWarning("Tasks for clients {FaultedClients} from groups {FaultedGroups} failed for query {Query}, swapped with result for {SuccessfulClient} in {ElapsedMilliseconds}ms", faultedClientsString, faultedGroupsString, query, queries[winningTask], sw.ElapsedMilliseconds);
                }
            }

            // Await the winning task (if it's faulted, it will throw at this point)
            var winningAnswer = await winningTask;

            // Only bother working out the logging if needed
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var winningClient = queries[winningTask];
                var winningGroup = randomisedClients[winningClient];
                _logger.LogTrace("Winning client was {WinningClient} from group {WinningGroup} for query {Query} in {ElapsedMilliseconds}ms", winningClient, winningGroup, query, sw.ElapsedMilliseconds);
            }

            return winningAnswer;
        }

        private async Task<DnsMessage> QueryWrapped(IDnsClient client, DnsMessage query, CancellationToken token)
        {
            var answer = await client.Query(query, token);
            answer.EnsureSuccessResponseCode();
            return answer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
