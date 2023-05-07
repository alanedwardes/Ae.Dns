using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsQueryFactoryTests
    {
        [Fact]
        public void TestGenerateId()
        {
            var randomIds = Enumerable.Range(0, ushort.MaxValue).Select(x => DnsQueryFactory.GenerateId());

            // Check the distribution is between 10% and 100% of the available values
            Assert.InRange(randomIds.Distinct().Count(), ushort.MaxValue / 10, ushort.MaxValue);
        }

        [Fact]
        public void TestCreateQuery()
        {
            var header = DnsQueryFactory.CreateQuery("example.com", DnsQueryType.CNAME);

            Assert.Equal("example.com", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.CNAME, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv4()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("1.2.3.4"));

            Assert.Equal("4.3.2.1.in-addr.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6_Example1()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2600:9000:2015:3600:1a:36dc:e5c0:93a1"));

            Assert.Equal("1.a.3.9.0.c.5.e.c.d.6.3.a.1.0.0.0.0.6.3.5.1.0.2.0.0.0.9.0.0.6.2.ip6.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6_Example2()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2a02:2e0:3fe:1001:302::"));

            Assert.Equal("0.0.0.0.0.0.0.0.0.0.0.0.2.0.3.0.1.0.0.1.e.f.3.0.0.e.2.0.2.0.a.2.ip6.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6_Example3()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2a00:1450:4009:815::200e"));

            Assert.Equal("e.0.0.2.0.0.0.0.0.0.0.0.0.0.0.0.5.1.8.0.9.0.0.4.0.5.4.1.0.0.a.2.ip6.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6_Example4()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2620:149:af0::10"));

            Assert.Equal("0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.f.a.0.9.4.1.0.0.2.6.2.ip6.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6_Example5()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2a02:26f0:fd00:11a8::1ea2"));

            Assert.Equal("2.a.e.1.0.0.0.0.0.0.0.0.0.0.0.0.8.a.1.1.0.0.d.f.0.f.6.2.2.0.a.2.ip6.arpa", header.Header.Host);
            Assert.True(header.Header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.Header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.Header.OperationCode);
            Assert.Equal(1, header.Header.QuestionCount);
            Assert.True(header.Header.RecursionDesired);
        }

        [Fact]
        public void TestTruncateAnswer()
        {
            var sampleAnswers = SampleDnsPackets.AnswerBatch1.Select(x => DnsByteExtensions.FromBytes<DnsMessage>(x));

            var knownTruncatedAnswer = sampleAnswers.Where(x => x.Header.Truncation).First();
            var knownNonTruncatedAnswer = sampleAnswers.Where(x => !x.Header.Truncation).First();

            var truncatedAnswer = DnsQueryFactory.TruncateAnswer(knownNonTruncatedAnswer);

            Assert.Equal(knownTruncatedAnswer.Header.Flags, truncatedAnswer.Header.Flags);
        }
    }
}
