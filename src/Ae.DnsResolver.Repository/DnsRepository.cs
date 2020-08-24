using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
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

        private byte[] CreateNullResponse(DnsHeader request) => CreateNullHeader(request).WriteDnsHeader().ToArray();

        public async Task<byte[]> Resolve(byte[] query)
        {
            int offset = 0;
            var header = query.ReadDnsHeader(ref offset);

            if (!_dnsFilter.IsPermitted(header))
            {
                _logger.LogTrace("DNS query blocked for {Domain}", header.Host);
                return CreateNullResponse(header);
            }

            byte[] answer;

            string cacheKey = GetCacheKey(header);

            var cached = _objectCache.Get(cacheKey);
            if (cached != null)
            {
                _logger.LogTrace("Returned cached DNS result for {Domain}", header.Host);

                answer = (byte[])cached;

                // Replace the ID
                answer[0] = query[0];
                answer[1] = query[1];

                return answer;
            }

            answer = await _dnsClient.LookupRaw(query);

            offset = 0;
            var answerMessage = answer.ReadDnsAnswer(ref offset);

            _logger.LogTrace("Returned fresh DNS result for {Domain}", header.Host);

            if (answerMessage.Answers.Length > 0)
            {
                var lowestTtl = answerMessage.Answers.Min(x => x.Ttl);

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
