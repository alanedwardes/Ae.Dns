using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Xunit;

namespace Ae.Dns.Tests.Server
{
    public class DnsCompositeFilterTests
    {
        [Fact]
        public void TestAllowedByDefault()
        {
            var dnsFilter = new DnsCompositeFilter();

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestAllowed()
        {
            var filter1 = new DnsDelegateFilter(x => false);
            var filter2 = new DnsDelegateFilter(x => true);
            
            var dnsFilter = new DnsCompositeFilter(filter1, filter2);

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestDisallowed()
        {
            var filter1 = new DnsDelegateFilter(x => false);
            var filter2 = new DnsDelegateFilter(x => false);

            var dnsFilter = new DnsCompositeFilter(filter1, filter2);

            Assert.False(dnsFilter.IsPermitted(new DnsHeader()));
        }
    }
}
