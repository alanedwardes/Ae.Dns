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

            var answer = Assert.Single(message.Answers);
            Assert.Null(answer.Resource);

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.NameIsNotInUse, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.AddToAnRRset, message.GetZoneUpdateType());
        }

        [Fact]
        public void ReadUpdate2()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            Assert.Null(message.Answers[0].Resource);
            Assert.Null(message.Answers[1].Resource);

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.RRsetExistsValueDependent, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.DeleteAnRRFromAnRRset, message.GetZoneUpdateType());
        }

        [Fact]
        public void ReadUpdate3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);

            var answer = Assert.Single(message.Answers);
            Assert.Null(answer.Resource);

            Assert.Equal(DnsMessageExtensions.ZoneUpdatePreRequisite.NameIsNotInUse, message.GetZoneUpdatePreRequisite());
            Assert.Equal(DnsMessageExtensions.ZoneUpdateType.AddToAnRRset, message.GetZoneUpdateType());
        }
    }
}
