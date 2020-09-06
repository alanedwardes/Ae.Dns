using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Client.Filters
{
    public class DnsCompositeFilterTests
    {
        [Fact]
        public void TestOrAllowedByDefault()
        {
            var dnsFilter = new DnsCompositeOrFilter();

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestOrAllowed()
        {
            var filter1 = new DnsDelegateFilter(x => false);
            var filter2 = new DnsDelegateFilter(x => true);
            
            var dnsFilter = new DnsCompositeOrFilter(filter1, filter2);

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestOrDisallowed()
        {
            var filter1 = new DnsDelegateFilter(x => false);
            var filter2 = new DnsDelegateFilter(x => false);

            var dnsFilter = new DnsCompositeOrFilter(filter1, filter2);

            Assert.False(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestAndAllowedByDefault()
        {
            var dnsFilter = new DnsCompositeAndFilter();

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestAndAllowed()
        {
            var filter1 = new DnsDelegateFilter(x => true);
            var filter2 = new DnsDelegateFilter(x => true);

            var dnsFilter = new DnsCompositeAndFilter(filter1, filter2);

            Assert.True(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestAndDisallowed()
        {
            var filter1 = new DnsDelegateFilter(x => false);
            var filter2 = new DnsDelegateFilter(x => false);

            var dnsFilter = new DnsCompositeAndFilter(filter1, filter2);

            Assert.False(dnsFilter.IsPermitted(new DnsHeader()));
        }

        [Fact]
        public void TestAndDisallowedOneTrue()
        {
            var filter1 = new DnsDelegateFilter(x => true);
            var filter2 = new DnsDelegateFilter(x => false);

            var dnsFilter = new DnsCompositeAndFilter(filter1, filter2);

            Assert.False(dnsFilter.IsPermitted(new DnsHeader()));
        }
    }
}
