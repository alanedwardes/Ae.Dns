using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsUpdateTests
    {
        [Fact]
        public void ReadUpdate1()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update1);
        }

        [Fact]
        public void ReadUpdate2()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);
        }

        [Fact]
        public void ReadUpdate3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);
        }
    }
}
