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

        private static readonly Meter _meter = new Meter("Ae.Dns.Client.DnsCachingClient");
        private static readonly Counter<int> _cacheHitCounter = _meter.CreateCounter<int>("Hit");
        private static readonly Counter<int> _cacheMissCounter = _meter.CreateCounter<int>("Miss");

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
        }

        private string GetCacheKey(DnsHeader header) => $"{header.Host}~{header.QueryType}~{header.QueryClass}";

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            var queryMetricState = new KeyValuePair<string, object>("Query", query);

            string cacheKey = GetCacheKey(query);

            DnsAnswer answer;

            var cacheEntry = (DnsCacheEntry)_objectCache.Get(cacheKey);
            if (cacheEntry != null)
            {
                _logger.LogTrace("Returned cached DNS result for {Domain} (expires in: {ExpiryTime})", query.Host, cacheEntry.Expires);

                answer = DnsByteExtensions.FromBytes<DnsAnswer>(cacheEntry.Data);
                
                // Replace the ID
                answer.Header.Id = query.Id;
                
                // Adjust the TTLs to be correct
                foreach (var record in answer.Answers)
                {
                    record.TimeToLive -= cacheEntry.Age;
                }

                _cacheHitCounter.Add(1, queryMetricState);
                return answer;
            }

            answer = await _dnsClient.Query(new DnsHeader
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
                cacheEntry = new DnsCacheEntry(answer);
                _objectCache.Add(cacheKey, cacheEntry, new CacheItemPolicy { AbsoluteExpiration = cacheEntry.Expiry });
            }

            _cacheMissCounter.Add(1, queryMetricState);
            return answer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
