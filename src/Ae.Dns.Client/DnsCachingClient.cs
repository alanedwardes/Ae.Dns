using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private sealed class DnsCacheEntry
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

        private readonly ILogger<DnsCachingClient> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;
        private readonly DnsCachingClientConfiguration _configuration;

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
        /// Construct a new caching DNS client using the specified client to
        /// forward requests to, and the specified memory cache.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use.</param>
        /// <param name="dnsClient">The see <see cref="IDnsClient"/> to delegate uncached requests to.</param>
        /// <param name="objectCache">The in-memory cache to use.</param>
        public DnsCachingClient(ILogger<DnsCachingClient> logger, IDnsClient dnsClient, ObjectCache objectCache) :
            this(logger, dnsClient, objectCache, new DnsCachingClientConfiguration())
        {
        }

        /// <summary>
        /// Construct a new caching DNS client using the specified logger,
        /// DNS client to forward requests to, and the specified memory cache.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use.</param>
        /// <param name="dnsClient">The see <see cref="IDnsClient"/> to delegate uncached requests to.</param>
        /// <param name="objectCache">The in-memory cache to use.</param>
        public DnsCachingClient(ILogger<DnsCachingClient> logger, IDnsClient dnsClient, ObjectCache objectCache, DnsCachingClientConfiguration configuration)
        {
            _logger = logger;
            _dnsClient = dnsClient;
            _objectCache = objectCache;
            _configuration = configuration;

            _meter = new Meter($"Ae.Dns.Client.DnsCachingClient.{objectCache.Name}");
            _cacheHitCounter = _meter.CreateCounter<int>("Hit");
            _cacheMissCounter = _meter.CreateCounter<int>("Miss");
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var queryMetricState = new KeyValuePair<string, object>("Query", query);

            if (TryGetAnswer(query, out DnsAnswer answer, out var expires))
            {
                foreach (var record in answer.Answers)
                {
                    // We can return stale data when uing the AdditionalTimeToLive option
                    if (record.TimeToLive < TimeSpan.Zero)
                    {
                        // Give the server time to refresh the cache entry
                        record.TimeToLive = TimeSpan.FromSeconds(10);
                    }
                }

                if (expires < TimeSpan.Zero)
                {
                    _logger.LogDebug("Cached DNS result for {Domain} is stale, refreshing in the background", query.Host);
                    _ = RefreshCache(query, token);
                }

                _logger.LogDebug("Returned cached DNS result for {Domain} (expires: {Expires})", query.Host, expires);
                _cacheHitCounter.Add(1, queryMetricState);
                return answer;
            }

            _cacheMissCounter.Add(1, queryMetricState);
            return await RefreshCache(query, token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        public string GetCacheKey(DnsHeader header) => $"{header.Host}~{header.QueryType}~{header.QueryClass}";

        public async Task<DnsAnswer> RefreshCache(DnsHeader query, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            var answer = await _dnsClient.Query(query, token);
            if (answer.Answers.Count > 0)
            {
                var cacheEntry = new DnsCacheEntry(answer);
                _objectCache.Set(GetCacheKey(query), cacheEntry, new CacheItemPolicy
                {
                    AbsoluteExpiration = cacheEntry.Expiry + _configuration.AdditionalTimeToLive
                });
                _logger.LogDebug("Refreshed cache for {Domain} in {ElapsedMilliseconds}ms", query.Host, sw.ElapsedMilliseconds);
            }

            return answer;
        }

        public bool TryGetAnswer(DnsHeader query, out DnsAnswer answer, out TimeSpan expires)
        {
            var entry = (DnsCacheEntry)_objectCache.Get(GetCacheKey(query));
            if (entry == null)
            {
                answer = null;
                expires = TimeSpan.Zero;
                return false;
            }

            answer = DnsByteExtensions.FromBytes<DnsAnswer>(entry.Data);

            // Replace the ID
            answer.Header.Id = query.Id;

            // Adjust the TTLs to be correct
            foreach (var record in answer.Answers)
            {
                record.TimeToLive -= entry.Age;
            }

            expires = entry.Expires;
            return true;
        }
    }
}
