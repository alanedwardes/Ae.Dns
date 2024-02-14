using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System.Linq;
using Xunit;

#pragma warning disable CS0618

namespace Ae.Dns.Tests.Zone
{
    public sealed class DnsZoneExtensionsTests
    {
        [Fact]
        public void TestUpdate6ModifyZone()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update6);

            var zone = new DnsZone();

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.NoError, validation);

            var updateResult = zone.Update(records => zone.PerformZoneUpdates(records, message)).GetAwaiter().GetResult();

            Assert.Equal(DnsResponseCode.NoError, updateResult);

            Assert.Equal(zone.Records, message.Nameservers);
        }

        [Fact]
        public void TestUpdate6PreReqsFail()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update6);

            var zone = new DnsZone(new[]
            {
                new DnsResourceRecord{ Host = "XR18-0F-B2-4E.home", Type = DnsQueryType.MX, Class = DnsQueryClass.IN }
            });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.YXDomain, validation);
        }

        [Fact]
        public void TestUpdate2PreReqsFail()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            var zone = new DnsZone(new[]
            {
                new DnsResourceRecord{ Host = "XR18-0F-B2-4E.home", Type = DnsQueryType.A, Class = DnsQueryClass.IN }
            });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.YXRRSet, validation);
        }

        [Fact]
        public void TestUpdate2ModifyZone()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            var zone = new DnsZone(new[]
            {
                new DnsResourceRecord{ Host = "XR18-0F-B2-4E.home", Type = DnsQueryType.TEXT, Class = DnsQueryClass.IN, Resource = new DnsTextResource{Entries = "00e35ba814529c9bbcd3de4e9b7d23e967" } }
            });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.NoError, validation);

            var updateResult = zone.Update(records => zone.PerformZoneUpdates(records, message)).GetAwaiter().GetResult();

            Assert.Equal(DnsResponseCode.NoError, updateResult);

            Assert.Empty(zone.Records);
        }

        [Fact]
        public void TestUpdate7PreReqsFail()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update7);

            var zone = new DnsZone(new[]
            {
                new DnsResourceRecord{ Host = "nuc.home", Type = DnsQueryType.A, Class = DnsQueryClass.IN }
            });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.YXDomain, validation);
        }

        [Fact]
        public void TestUpdate7ModifyZone()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update7);

            var zone = new DnsZone(new[]
            {
                new DnsResourceRecord{ Host = "XR18-0F-B2-4E.home", Type = DnsQueryType.TEXT, Class = DnsQueryClass.IN, Resource = new DnsTextResource{Entries = "00e35ba814529c9bbcd3de4e9b7d23e967" } }
            });

            var validation = zone.TestZoneUpdatePreRequisites(message);

            Assert.Equal(DnsResponseCode.NoError, validation);

            var updateResult = zone.Update(records => zone.PerformZoneUpdates(records, message)).GetAwaiter().GetResult();

            Assert.Equal(DnsResponseCode.NoError, updateResult);

            Assert.Equal(new[] { zone.Records[0] }.Concat(message.Nameservers), zone.Records);
        }
    }
}
