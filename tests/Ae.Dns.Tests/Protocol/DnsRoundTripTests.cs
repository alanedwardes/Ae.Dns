using Xunit;
using Ae.Dns.Protocol;
using System.Linq;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsRoundTripTests
    {
        [Theory]
        [ClassData(typeof(QueryTheoryData))]
        public void TestRoundTripQueries(byte[] queryBytes)
        {
            var query = queryBytes.FromBytes<DnsHeader>();
            Assert.Equal(query, query.ToBytes().ToArray().FromBytes<DnsHeader>());
        }

        [Theory]
        [ClassData(typeof(AnswerTheoryData))]
        public void TestRoundTripAnswers(byte[] answerBytes)
        {
            var answer = answerBytes.FromBytes<DnsAnswer>();
            Assert.Equal(answer, answer.ToBytes().ToArray().FromBytes<DnsAnswer>());
        }
    }
}
