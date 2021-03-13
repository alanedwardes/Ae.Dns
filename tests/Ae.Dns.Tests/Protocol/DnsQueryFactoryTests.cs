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

            Assert.Equal("example.com", header.Host);
            Assert.True(header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.CNAME, header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.OperationCode);
            Assert.Equal(1, header.QuestionCount);
            Assert.True(header.RecusionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv4()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("1.2.3.4"));

            Assert.Equal("4.3.2.1.in-addr.arpa", header.Host);
            Assert.True(header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.OperationCode);
            Assert.Equal(1, header.QuestionCount);
            Assert.True(header.RecusionDesired);
        }

        [Fact]
        public void TestCreateReverseQueryIPv6()
        {
            var header = DnsQueryFactory.CreateReverseQuery(IPAddress.Parse("2600:9000:2015:3600:1a:36dc:e5c0:93a1"));

            Assert.Equal("1.a.3.9.0.c.5.e.c.d.6.3.a.1.0.0.6.3.5.1.0.2.0.0.0.9.0.0.6.2.ip6.arpa", header.Host);
            Assert.True(header.Id > 0);
            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, header.QueryType);
            Assert.Equal(DnsOperationCode.QUERY, header.OperationCode);
            Assert.Equal(1, header.QuestionCount);
            Assert.True(header.RecusionDesired);
        }
    }
}
