using Xunit;
using Ae.DnsResolver.Protocol;
using System.Linq;

namespace Ae.DnsResolver.Tests.Protocol
{
    public class DnsRoundTripTests
    {
        [Theory]
        [ClassData(typeof(QueryTheoryData))]
        public void TestRoundTripQueries(byte[] queryBytes)
        {
            var query = queryBytes.ReadDnsHeader();
            Assert.Equal(query, query.ToBytes().ToArray().ReadDnsHeader());
        }

        [Theory]
        [ClassData(typeof(AnswerTheoryData))]
        public void TestRoundTripAnswers(byte[] answerBytes)
        {
            var answer = answerBytes.ReadDnsAnswer();
            Assert.Equal(answer, answer.ToBytes().ToArray().ReadDnsAnswer());
        }
    }
}
