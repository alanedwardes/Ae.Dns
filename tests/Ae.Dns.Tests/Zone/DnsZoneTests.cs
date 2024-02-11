using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Zone
{
    [Obsolete]
    public sealed class DnsZoneTests
    {
        private IDnsZone _wikipediaExampleZone = new DnsZone(new[]
        {
            new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.SOA,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsSoaResource
                {
                    MName = "ns.example.com",
                    RName = "username.example.com",
                    Serial = 2020091025,
                    Refresh = TimeSpan.FromSeconds(7200),
                    Retry = TimeSpan.FromSeconds(3600),
                    Expire = TimeSpan.FromSeconds(1209600),
                    Minimum = TimeSpan.FromSeconds(3600)
                }
            },
            new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.NS,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "ns.example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.NS,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "ns.somewhere.example" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 10, Entries = "mail.example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 20, Entries = "mail2.example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 50, Entries = "mail3.example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.1") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.AAAA,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("2001:db8:10::1") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "ns.example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.2") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.AAAA,
                Host = "ns.example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("2001:db8:10::2") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "www.example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "wwwtest.example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "www.example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail.example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.3") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail2.example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.4") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail3.example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.5") }
            }
        })
        {
            Origin = "example.com",
            DefaultTtl = TimeSpan.FromHours(1)
        };

        [Fact]
        public async Task TestRoundTripZone()
        {
            var zone = await RoundTripRecords(_wikipediaExampleZone.Records.ToArray());

            Assert.NotNull(zone);
        }

        [Fact]
        public async Task TestTxtResource()
        {
            var zone = await RoundTripRecords(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.TEXT,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsTextResource
                {
                    Entries = new Dns.Protocol.DnsLabels(new[] { "test1", "test2" })
                }
            },
            new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.TEXT,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsTextResource
                {
                    Entries = "test"
                }
            });

            Assert.NotNull(zone);
        }

        [Fact]
        public async Task TestLoadZone1()
        {
            var zone = new DnsZone();
            zone.DeserializeZone(await File.ReadAllTextAsync("Zone/test1.zone"));

            Assert.Equal(zone.Records, _wikipediaExampleZone.Records);

            var serialized = await RoundTripRecords(zone.Records.ToArray());

            Assert.NotNull(serialized);
        }

        [Fact]
        public async Task TestLoadZone2()
        {
            var zone = new DnsZone();
            zone.DeserializeZone(await File.ReadAllTextAsync("Zone/test2.zone"));
        }

        [Fact]
        public async Task TestLoadZone3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Update3);

            // This packet has bad hostnames
            message.Nameservers[0].Host = "eufyRoboVac.home";
            message.Nameservers[1].Host = "eufyRoboVac.home";

            var zone = new DnsZone(message.Nameservers);

            var serialized = await RoundTripRecords(zone.Records.ToArray());

            Assert.NotNull(serialized);
        }

        public async Task<string> RoundTripRecords(params DnsResourceRecord[] records)
        {
            var originalZone = new DnsZone
            {
                Origin = "example.com",
                DefaultTtl = TimeSpan.FromSeconds(3600)
            };

            await originalZone.Update(recordsToUpdate =>
            {
                foreach (var record in records)
                {
                    recordsToUpdate.Add(record);
                }
            });

            var serialized = originalZone.SerializeZone();

            var newZone = new DnsZone();
            newZone.DeserializeZone(serialized);

            Assert.Equal(originalZone.Records, newZone.Records);

            return newZone.SerializeZone();
        }
    }
}
