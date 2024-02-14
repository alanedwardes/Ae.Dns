using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Zone;
using System.Linq;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Ae.Dns.Tests.Protocol
{
    public class DnsUpdateTests
    {
        [Fact]
        public void ReadUpdate1()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update1);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal("kitchensensor 0 QCLASS_NONE ANY", message.Answers.Single().ToZone(zone));

            Assert.Equal(2, message.Nameservers.Count);
            Assert.Equal("kitchensensor 3600 IN A 192.168.178.167", message.Nameservers[0].ToZone(zone));
            Assert.Equal("kitchensensor 3600 IN TEXT 009e7fac2e7a48556c1319fc22c2c1fc04", message.Nameservers[1].ToZone(zone));
        }

        [Fact]
        public void ReadUpdate2()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update2);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal(2, message.Answers.Count);
            Assert.Equal("XR18-0F-B2-4E 0 QCLASS_NONE A", message.Answers[0].ToZone(zone));
            Assert.Equal("XR18-0F-B2-4E 0 QCLASS_NONE AAAA", message.Answers[1].ToZone(zone));

            Assert.Equal("XR18-0F-B2-4E 0 QCLASS_NONE TEXT 00e35ba814529c9bbcd3de4e9b7d23e967", message.Nameservers.Single().ToZone(zone));
        }

        [Fact]
        public void ReadUpdate3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);

            var answer = Assert.Single(message.Answers);
            Assert.Null(answer.Resource);
        }

        [Fact]
        public void ReadUpdate4()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update4);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal("SoundTouch-Kitchen-Speaker 0 QCLASS_NONE ANY", message.Answers.Single().ToZone(zone));

            Assert.Equal(2, message.Nameservers.Count);
            Assert.Equal("SoundTouch-Kitchen-Speaker 3600 IN A 192.168.178.22", message.Nameservers[0].ToZone(zone));
            Assert.Equal("SoundTouch-Kitchen-Speaker 3600 IN DHCID (AAEBUZoX6pGOiVOe8e0lTNwDJAQ0Hy5dDWDM+ffKxPzbZF8=)", message.Nameservers[1].ToZone(zone));
        }

        [Fact]
        public void ReadUpdate5()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update5);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal("media 0 QCLASS_NONE ANY", message.Answers.Single().ToZone(zone));

            Assert.Equal(2, message.Nameservers.Count);
            Assert.Equal("media 3600 IN A 192.168.178.27", message.Nameservers[0].ToZone(zone));
            Assert.Equal("media 3600 IN DHCID (AAIBroGQ43nqbF6rT2by1rqALNv0LS/VfDLEFXKWOt0p4QM=)", message.Nameservers[1].ToZone(zone));
        }

        [Fact]
        public void ReadUpdate6()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update6);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal("XR18-0F-B2-4E 0 QCLASS_NONE ANY", message.Answers.Single().ToZone(zone));

            Assert.Equal(2, message.Nameservers.Count);
            Assert.Equal("XR18-0F-B2-4E 3600 IN A 192.168.178.174", message.Nameservers[0].ToZone(zone));
            Assert.Equal("XR18-0F-B2-4E 3600 IN DHCID (AAABqMU+3gu7C0DcMfgNnTqTq4t+xmzzqPaTY3rrjF4Tvvs=)", message.Nameservers[1].ToZone(zone));
        }

        [Fact]
        public void ReadUpdate7()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update7);

            var zone = new DnsZone { Origin = "home" };

            Assert.Equal("nuc 0 QCLASS_NONE ANY", message.Answers.Single().ToZone(zone));

            Assert.Equal(2, message.Nameservers.Count);
            Assert.Equal("nuc 3600 IN A 192.168.178.5", message.Nameservers[0].ToZone(zone));
            Assert.Equal("nuc 3600 IN DHCID (AAIBpsbVTmzjTQGmnXepaN82N79Wi1P9aDlYUNirVwBnu30=)", message.Nameservers[1].ToZone(zone));
        }
    }
}
