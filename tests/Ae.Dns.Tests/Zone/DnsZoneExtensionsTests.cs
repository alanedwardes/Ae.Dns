using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using Xunit;

namespace Ae.Dns.Tests.Zone
{
    public sealed class DnsZoneExtensionsTests
    {
        [Fact]
        [System.Obsolete]
        public void TestResourceSetDoesNotExist()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            var zone = new DnsZone();

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.NoError, validation);
        }

        [Fact]
        [System.Obsolete]
        public void TestResourceSetDoesExist()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            var zone = new DnsZone(message.Answers);

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.YXRRSet, validation);
        }

        [Fact]
        [System.Obsolete]
        public void TestNameIsNotInUseSucceed()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);

            var zone = new DnsZone(new[] { new DnsResourceRecord { Class = DnsQueryClass.IN } });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.NoError, validation);
        }

        [Fact]
        [System.Obsolete]
        public void TestNameIsNotInUseFail()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update1);

            var zone = new DnsZone(new[] { new DnsResourceRecord { Host = "kitchensensor.home" } });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.YXDomain, validation);
        }
    }
}
