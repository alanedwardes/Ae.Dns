using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public sealed class DnsRepository : IDnsRepository
    {
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;
        private readonly IReadOnlyCollection<IDnsFilter> _dnsFilters;

        private string GetCacheKey(DnsHeader header) => $"{string.Join(".", header.Labels)}~{header.Qtype}~{header.Qclass}";

        public DnsRepository(IDnsClient dnsClient, ObjectCache objectCache, IReadOnlyCollection<IDnsFilter> dnsFilters)
        {
            _dnsClient = dnsClient;
            _objectCache = objectCache;
            _dnsFilters = dnsFilters;
        }

        private byte[] CreateNullResponse(DnsHeader request)
        {
            var header = new DnsHeader
            {
                Id = request.Id,
                Header = 33155,
                Labels = request.Labels,
                Qclass = request.Qclass,
                Qdcount = request.Qdcount,
                Qtype = request.Qtype
            };

            return header.WriteDnsHeader().ToArray();
        }

        public async Task<byte[]> Resolve(byte[] query)
        {
            int offset = 0;
            var header = query.ReadDnsHeader(ref offset);

            var domain = string.Join(".", header.Labels);

            foreach (var filter in _dnsFilters)
            {
                if (!filter.IsPermitted(domain))
                {
                    Console.WriteLine($"BLOCKED DOMAIN: {domain}");
                    return CreateNullResponse(header);
                }
            }

            byte[] answer;

            string cacheKey = GetCacheKey(header);

            var cached = _objectCache.Get(cacheKey);
            if (cached != null)
            {
                answer = (byte[])cached;

                // Replace the ID
                answer[0] = query[0];
                answer[1] = query[1];

                return answer;
            }

            answer = await _dnsClient.LookupRaw(query);

            offset = 0;
            var answerMessage = DnsMessageReader.ReadDnsResponse(answer, ref offset);

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
