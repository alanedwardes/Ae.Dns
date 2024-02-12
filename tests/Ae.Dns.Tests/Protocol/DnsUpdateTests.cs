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

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.NameIsNotInUse, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.AddToAnRRset, message.GetZoneUpdateType());
        }

        [Fact]
        public void ReadUpdate2()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.RRsetExistsValueDependent, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.DeleteAnRRFromAnRRset, message.GetZoneUpdateType());
        }

        [Fact]
        public void ReadUpdate3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.NameIsNotInUse, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.AddToAnRRset, message.GetZoneUpdateType());
        }
    }
}
