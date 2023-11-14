using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ae.Dns.Client
{
    [Obsolete("This class is experimental")]
    public sealed class DnsHttpsRecordGeneratorClient : IDnsClient
    {
        private readonly IDnsClient _innerClient;
        private readonly HttpClient _httpClient;

        public DnsHttpsRecordGeneratorClient(IDnsClient innerClient, HttpClient httpClient)
        {
            _innerClient = innerClient;
            _httpClient = httpClient;
        }

        public void Dispose() => _httpClient.Dispose();

        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            if (query.Header.QueryType == DnsQueryType.HTTPS)
            {
                var innerAnswer = await _innerClient.Query(query, token);
                var httpsAnswers = innerAnswer.Answers.Where(x => x.Type == DnsQueryType.HTTPS).ToArray();
                if (httpsAnswers.Any())
                {
                    return innerAnswer;
                }

                using var response = await _httpClient.GetAsync("https://" + query.Header.Host);
                if (response.IsSuccessStatusCode)
                {

                }
            }

            return await _innerClient.Query(query, token);
        }
    }
}
