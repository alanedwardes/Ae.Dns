using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Xunit;

namespace Ae.Dns.Tests.Client.Filters
{
    public class DnsLocalNetworkQueryFilterTests
    {
        [Theory]
        [InlineData("google.com", DnsQueryType.A, true)]
        [InlineData("lb._dns-sd._udp.0.178.168.192.in-addr.arpa", DnsQueryType.PTR, false)]
        [InlineData("test.home", DnsQueryType.A, false)]
        [InlineData("_rocketchat._https", DnsQueryType.SRV, false)]
        [InlineData("rocketchat-tcp-protocol", DnsQueryType.TEXT, false)]
        [InlineData("rocketchat-public-key", DnsQueryType.TEXT, false)]
        [InlineData("local", DnsQueryType.SOA, false)]
        [InlineData("216.58.212.238.in-addr.arpa", DnsQueryType.PTR, true)]
        [InlineData("23.178.168.192.in-addr.arpa", DnsQueryType.PTR, false)]
        [InlineData("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.f.f.f.f.0.0.0.0.0.0.0.0.0.c.e.f.ip6.arpa", DnsQueryType.PTR, false)]
        [InlineData("2.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.f.f.f.f.0.0.0.0.0.0.0.0.0.c.e.f.ip6.arpa", DnsQueryType.PTR, false)]
        [InlineData("4.f.d.b.0.d.a.f.2.e.d.c.f.0.8.8.0.0.0.0.0.0.0.0.0.0.0.0.0.8.e.f.ip6.arpa", DnsQueryType.PTR, false)]
        [InlineData("e.0.0.2.0.0.0.0.0.0.0.0.0.0.0.0.1.2.8.0.9.0.0.4.0.5.4.1.0.0.a.2.ip6.arpa", DnsQueryType.PTR, true)]
        [InlineData("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa", DnsQueryType.PTR, false)]
        public void TestNormalLookup(string host, DnsQueryType queryType, bool isPermitted)
        {
            var message = DnsQueryFactory.CreateQuery(host, queryType);

            var filter = new DnsLocalNetworkQueryFilter();

            Assert.Equal(isPermitted, filter.IsPermitted(message));
        }
    }
}
