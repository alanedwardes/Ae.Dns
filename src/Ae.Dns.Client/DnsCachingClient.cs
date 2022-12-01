using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a DNS client which provides in-memory caching facilities,
    /// forwarding uncached DNS queries to another client.
    /// </summary>
    public sealed class DnsCachingClient : IDnsClient
    {
        private class DnsCacheEntry
        {
            public DnsCacheEntry(DnsMessage answer)
            {
                var allRecords = new[] { answer.Answers, answer.Nameservers };

                LowestRecordTimeToLive = TimeSpan.FromSeconds(allRecords.SelectMany(x => x).Min(x => x.TimeToLive));
                Data = DnsByteExtensions.AllocateAndWrite(answer);
            }

            public DateTimeOffset Time { get; } = DateTimeOffset.UtcNow;
            public ReadOnlyMemory<byte> Data { get; }
            public TimeSpan LowestRecordTimeToLive { get; }
            public DateTimeOffset Expiry => Time + LowestRecordTimeToLive;
            public TimeSpan Age => DateTimeOffset.UtcNow - Time;
            public TimeSpan Expires => Expiry - DateTimeOffset.UtcNow;
        }

        private readonly ILogger<DnsCachingClient> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;

        /// <summary>
        /// Construct a new caching DNS client using the specified logger,
        /// DNS client to forward requests to, and the specified memory cache.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use.</param>
        /// <param name="dnsClient">The see <see cref="IDnsClient"/> to delegate uncached requests to.</param>
        /// <param name="objectCache">The in-memory cache to use.</param>
        public DnsCachingClient(ILogger<DnsCachingClient> logger, IDnsClient dnsClient, ObjectCache objectCache)
        {
            _logger = logger;
            _dnsClient = dnsClient;
            _objectCache = objectCache;
        }

        private string GetCacheKey(DnsMessage header)
        {
            var sb = new StringBuilder();
            sb.Append($"{header.Header.Host}~{header.Header.QueryType}~{header.Header.QueryClass}");

            foreach (var additional in header.Additional)
            {
                sb.Append($"{additional.Host}~{additional.Type}~{additional.Class}~{additional.TimeToLive}");
            }

            return sb.ToString();
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token)
        {
            var cachedAnswer = GetCachedAnswer(query);
            if (cachedAnswer != null)
            {
                cachedAnswer.Header.Tags.Add("Resolver", $"{nameof(DnsCachingClient)}({_objectCache.Name})");
                return cachedAnswer;
            }

            var freshAnswer = await GetFreshAnswer(query, token);
            return freshAnswer;
        }

        private DnsMessage GetCachedAnswer(DnsMessage query)
        {
            var cacheEntry = (DnsCacheEntry)_objectCache.Get(GetCacheKey(query));
            if (cacheEntry == null)
            {
                return null;
            }

            _logger.LogTrace("Returned cached DNS result for {Domain} (expires in: {ExpiryTime})", query.Header.Host, cacheEntry.Expires);

            var answer = DnsByteExtensions.FromBytes<DnsMessage>(cacheEntry.Data);

            // Replace the ID
            answer.Header.Id = query.Header.Id;

            // Adjust the TTLs to be correct
            foreach (var record in answer.Answers)
            {
                var recordTtl = TimeSpan.FromSeconds(record.TimeToLive);
                var newTtl = recordTtl - cacheEntry.Age;

                var entryHasFairlyLongTtl = recordTtl > TimeSpan.FromMinutes(5);
                var newTtlExpiresSoon = newTtl < TimeSpan.FromMinutes(5);

                if (entryHasFairlyLongTtl && newTtlExpiresSoon)
                {
                    // Prefetch DNS entries about to expire
                    _ = PrefetchAnswer(query);
                }

                record.TimeToLive = (uint)newTtl.TotalSeconds;
            }

            return answer;
        }

        private async Task PrefetchAnswer(DnsMessage query)
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                await GetFreshAnswer(query, tokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Prefetch failed");
                throw;
            }
        }

        private async Task<DnsMessage> GetFreshAnswer(DnsMessage query, CancellationToken token)
        {
            var answer = await _dnsClient.Query(new DnsMessage
            {
                Header = new DnsHeader
                {
                    Id = query.Header.Id,
                    Host = query.Header.Host,
                    IsQueryResponse = false,
                    RecusionDesired = query.Header.RecusionDesired,
                    QueryClass = query.Header.QueryClass,
                    QueryType = query.Header.QueryType,
                    QuestionCount = query.Header.QuestionCount,
                    AdditionalRecordCount = query.Header.AdditionalRecordCount
                },
                Additional = query.Additional
            }, token);

            _logger.LogTrace("Returned fresh DNS result for {Domain}", query.Header.Host);

            if (answer.Answers.Count + answer.Nameservers.Count + answer.Additional.Count > 0)
            {
                var cacheEntry = new DnsCacheEntry(answer);
                _objectCache.Add(GetCacheKey(query), cacheEntry, new CacheItemPolicy { AbsoluteExpiration = cacheEntry.Expiry });
            }

            return answer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsCachingClient)}({_objectCache.Name})";
    }
}
