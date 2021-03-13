using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Client.Filters
{
    public class DnsDomainSuffixFilterTests
    {
        [Fact]
        public void TestOrAllowedByDefault()
        {
            var dnsFilter = new DnsDomainSuffixFilter("example.com");

            Assert.False(dnsFilter.IsPermitted(DnsQueryFactory.CreateQuery("test.example.com")));
            Assert.True(dnsFilter.IsPermitted(DnsQueryFactory.CreateQuery("test.example.org")));
            Assert.False(dnsFilter.IsPermitted(DnsQueryFactory.CreateQuery("test.example.org.example.com")));
            Assert.True(dnsFilter.IsPermitted(DnsQueryFactory.CreateQuery("test.example.com.example.org")));
        }
    }
}
