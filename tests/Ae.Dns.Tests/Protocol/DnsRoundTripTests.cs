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
            var query = DnsByteExtensions.FromBytes<DnsHeader>(queryBytes);
            Assert.Equal(query, DnsByteExtensions.FromBytes<DnsHeader>(DnsByteExtensions.ToBytes(query).ToArray()));
        }

        [Theory]
        [ClassData(typeof(AnswerTheoryData))]
        public void TestRoundTripAnswers(byte[] answerBytes)
        {
            var answer = DnsByteExtensions.FromBytes<DnsAnswer>(answerBytes);
            Assert.Equal(answer, DnsByteExtensions.FromBytes<DnsAnswer>(DnsByteExtensions.ToBytes(answer).ToArray()));
        }
    }
}
