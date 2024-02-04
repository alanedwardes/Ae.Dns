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
    }
}
