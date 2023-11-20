using Ae.Dns.Client.Polly;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public static class ClientTestExtensions
    {
        public static async Task RunQuery(this IDnsClient client, string host, DnsQueryType queryType, DnsResponseCode expectedResponseCode = DnsResponseCode.NoError)
        {
            using var retry = new DnsPollyClient(client, Policy<DnsMessage>.Handle<Exception>().WaitAndRetryForeverAsync(x => TimeSpan.FromSeconds(x)));

            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var query = DnsQueryFactory.CreateQuery(host, queryType);
            var answer = await retry.Query(query, tokenSource.Token);
            Assert.Equal(host, answer.Header.Host.ToString());
            Assert.Equal(query.Header.Id, answer.Header.Id);
            Assert.Equal(expectedResponseCode, answer.Header.ResponseCode);

            if (expectedResponseCode == DnsResponseCode.NoError && !answer.Header.Truncation)
            {
                Assert.True(answer.Answers.Count > 0);
            }
            else
            {
                Assert.Empty(answer.Answers);
            }
        }
    }
}
