using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public sealed class DnsRepository : IDnsRepository
    {
        private readonly IDnsClient _dnsClient;
        private readonly ObjectCache _objectCache;
        private readonly IDnsFilter _dnsFilter;

        public DnsRepository(IDnsClient dnsClient, ObjectCache objectCache, IDnsFilter dnsFilter)
        {
            _dnsClient = dnsClient;
            _objectCache = objectCache;
            _dnsFilter = dnsFilter;
        }

        private string GetCacheKey(DnsHeader header) => $"{string.Join(".", header.Labels)}~{header.Qtype}~{header.Qclass}";

        private byte[] CreateNullResponse(DnsHeader request) => new DnsHeader
        {
            Id = request.Id,
            Header = 33155,
            Labels = request.Labels,
            Qclass = request.Qclass,
            Qdcount = request.Qdcount,
            Qtype = request.Qtype
        }.WriteDnsHeader().ToArray();

        public async Task<byte[]> Resolve(byte[] query)
        {
            int offset = 0;
            var header = query.ReadDnsHeader(ref offset);

            if (!_dnsFilter.IsPermitted(header))
            {
                Console.WriteLine($"BLOCKED DOMAIN: {header}");
                return CreateNullResponse(header);
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
