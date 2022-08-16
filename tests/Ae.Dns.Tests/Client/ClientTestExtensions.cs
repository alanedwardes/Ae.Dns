using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public static class ClientTestExtensions
    {
        public static async Task RunQuery(this IDnsClient client, string host, DnsQueryType queryType, DnsResponseCode expectedResponseCode = DnsResponseCode.NoError)
        {
            var query = DnsQueryFactory.CreateQuery(host, queryType);
            var answer = await client.Query(query);
            Assert.Equal(host, answer.Header.Host);
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
