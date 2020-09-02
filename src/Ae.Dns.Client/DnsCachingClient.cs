using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    public sealed class DnsCachingClient : IDnsClient
    {
        private class DnsCacheEntry
        {
            public DnsCacheEntry(DnsAnswer answer)
            {
                LowestRecordTimeToLive = answer.Answers.Min(x => x.TimeToLive);
                Data = answer.ToBytes();
            }

            public DateTimeOffset Time { get; } = DateTimeOffset.UtcNow;
            public byte[] Data { get; }
            public TimeSpan LowestRecordTimeToLive { get; }
            public DateTimeOffset Expiry => Time + LowestRecordTimeToLive;
            public TimeSpan Age => DateTime.UtcNow - Time;
            public TimeSpan Expires => Expiry - DateTimeOffset.UtcNow;
        }

        private readonly ILogger<DnsCachingClient> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;

        public DnsCachingClient(ILogger<DnsCachingClient> logger, IDnsClient dnsClient, ObjectCache objectCache)
        {
            _logger = logger;
            _dnsClient = dnsClient;
            _objectCache = objectCache;
        }

        private string GetCacheKey(DnsHeader header) => $"{header.Host}~{header.QueryType}~{header.QueryClass}";

        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token)
        {
            string cacheKey = GetCacheKey(query);

            DnsAnswer answer;

            var cacheEntry = (DnsCacheEntry)_objectCache.Get(cacheKey);
            if (cacheEntry != null)
            {
                _logger.LogTrace("Returned cached DNS result for {Domain} (expires in: {ExpiryTime})", query.Host, cacheEntry.Expires);

                answer = cacheEntry.Data.FromBytes<DnsAnswer>();
                
                // Replace the ID
                answer.Header.Id = query.Id;
                
                // Adjust the TTLs to be correct
                foreach (var record in answer.Answers)
                {
                    record.TimeToLive -= cacheEntry.Age;
                }

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

            return answer;
        }
    }
}
