using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Caching;
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
            public DnsCacheEntry(DnsAnswer answer)
            {
                LowestRecordTimeToLive = answer.Answers.Min(x => x.TimeToLive);
                Data = DnsByteExtensions.ToBytes(answer);
            }

            public DateTimeOffset Time { get; } = DateTimeOffset.UtcNow;
            public byte[] Data { get; }
            public TimeSpan LowestRecordTimeToLive { get; }
            public DateTimeOffset Expiry => Time + LowestRecordTimeToLive;
            public TimeSpan Age => DateTimeOffset.UtcNow - Time;
            public TimeSpan Expires => Expiry - DateTimeOffset.UtcNow;
        }

        private readonly Meter _meter;
        private readonly Counter<int> _cacheHitCounter;
        private readonly Counter<int> _cacheMissCounter;
        private readonly Counter<int> _cachePrefetchCounter;
        private readonly ILogger<DnsCachingClient> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;

        /// <summary>
        /// Construct a new caching DNS client using the specified client to
        /// forward requests to, and the specified memory cache.
        /// </summary>
        /// <param name="dnsClient">The see <see cref="IDnsClient"/> to delegate uncached requests to.</param>
        /// <param name="objectCache">The in-memory cache to use.</param>
        public DnsCachingClient(IDnsClient dnsClient, ObjectCache objectCache) :
            this(new NullLogger<DnsCachingClient>(), dnsClient, objectCache)
        {
        }

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

            _meter = new Meter($"Ae.Dns.Client.DnsCachingClient.{objectCache.Name}");
            _cacheHitCounter = _meter.CreateCounter<int>("Hit");
            _cacheMissCounter = _meter.CreateCounter<int>("Miss");
            _cachePrefetchCounter = _meter.CreateCounter<int>("Prefetch");
        }

        private string GetCacheKey(DnsHeader header) => $"{header.Host}~{header.QueryType}~{header.QueryClass}";

        private KeyValuePair<string, object> GetMetricState(DnsHeader query) => new KeyValuePair<string, object>("Query", query);

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var cachedAnswer = GetCachedAnswer(query);
            if (cachedAnswer != null)
            {
                _cacheHitCounter.Add(1, GetMetricState(query));
                return cachedAnswer;
            }

            _cacheMissCounter.Add(1, GetMetricState(query));
            return await GetFreshAnswer(query, token);
        }

        private DnsAnswer GetCachedAnswer(DnsHeader query)
        {
            var cacheEntry = (DnsCacheEntry)_objectCache.Get(GetCacheKey(query));
            if (cacheEntry == null)
            {
                return null;
            }

            _logger.LogTrace("Returned cached DNS result for {Domain} (expires in: {ExpiryTime})", query.Host, cacheEntry.Expires);

            var answer = DnsByteExtensions.FromBytes<DnsAnswer>(cacheEntry.Data);

            // Replace the ID
            answer.Header.Id = query.Id;

            // Adjust the TTLs to be correct
            foreach (var record in answer.Answers)
            {
                var newTtl = record.TimeToLive - cacheEntry.Age;

                var entryHasFairlyLongTtl = record.TimeToLive > TimeSpan.FromMinutes(5);
                var newTtlExpiresSoon = newTtl < TimeSpan.FromMinutes(5);

                if (entryHasFairlyLongTtl && newTtlExpiresSoon)
                {
                    // Prefetch DNS entries about to expire
                    _ = PrefetchAnswer(query);
                }

                record.TimeToLive = newTtl;
            }

            return answer;
        }

        private async Task PrefetchAnswer(DnsHeader query)
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

            _cachePrefetchCounter.Add(1, GetMetricState(query));
        }

        private async Task<DnsAnswer> GetFreshAnswer(DnsHeader query, CancellationToken token)
        {
            var answer = await _dnsClient.Query(new DnsHeader
            {
                Id = query.Id,
                Host = query.Host,
                IsQueryResponse = false,
                RecusionDesired = query.RecusionDesired,
                QueryClass = query.QueryClass,
                QueryType = query.QueryType,
                QuestionCount = query.QuestionCount
            }, token);

            _logger.LogTrace("Returned fresh DNS result for {Domain}", query.Host);

            if (answer.Answers.Count > 0)
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
    }
}
