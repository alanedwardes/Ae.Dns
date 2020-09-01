using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Ae.Dns.Repository
{
    public sealed class DnsRepository : IDnsRepository
    {
        private readonly ILogger<DnsRepository> _logger;
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;
        private readonly IDnsFilter _dnsFilter;

        public DnsRepository(ILogger<DnsRepository> logger, IDnsClient dnsClient, ObjectCache objectCache, IDnsFilter dnsFilter)
        {
            _logger = logger;
            _dnsClient = dnsClient;
            _objectCache = objectCache;
            _dnsFilter = dnsFilter;
        }

        private string GetCacheKey(DnsHeader header) => $"{header.Host}~{header.QueryType}~{header.QueryClass}";

        private DnsHeader CreateNullHeader(DnsHeader request)
        {
            return new DnsHeader
            {
                Id = request.Id,
                ResponseCode = DnsResponseCode.NXDomain,
                IsQueryResponse = true,
                RecursionAvailable = true,
                RecusionDesired = request.RecusionDesired,
                Host = request.Host,
                QueryClass = request.QueryClass,
                QuestionCount = request.QuestionCount,
                QueryType = request.QueryType
            };
        }

        public async Task<DnsAnswer> Resolve(DnsHeader query)
        {
            if (!_dnsFilter.IsPermitted(query))
            {
                _logger.LogTrace("DNS query blocked for {Domain}", query.Host);
                return new DnsAnswer
                {
                    Header = CreateNullHeader(query)
                };
            }

            DnsAnswer answer;

            string cacheKey = GetCacheKey(query);

            var cached = _objectCache.Get(cacheKey);
            if (cached != null)
            {
                _logger.LogTrace("Returned cached DNS result for {Domain}", query.Host);

                answer = (DnsAnswer)cached;

                // Replace the ID
                answer.Header.Id = query.Id;

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
            });

            _logger.LogTrace("Returned fresh DNS result for {Domain}", query.Host);

            if (answer.Answers.Count > 0)
            {
                var lowestTtl = answer.Answers.Min(x => x.TimeToLive);

                var cachePolicy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTime.Now + lowestTtl
                };

                _objectCache.Add(new CacheItem(cacheKey, answer), cachePolicy);
            }

            return answer;
        }
    }
}
