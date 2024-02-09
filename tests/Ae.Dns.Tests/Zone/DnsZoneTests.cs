using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Zone
{
    [Obsolete]
    public sealed class DnsZoneTests
    {
        [Fact]
        public async Task TestRoundTripZone()
        {
            var zone = await RoundTripRecords(new DnsResourceRecord
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
                Resource = new DnsDomainResource { Entries = "ns" }
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
                Resource = new DnsMxResource { Preference = 50, Entries = "mail3" }
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
                Host = "ns",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.2") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.AAAA,
                Host = "ns",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("2001:db8:10::2") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "www",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "example.com" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "wwwtest",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "www" }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.3") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail2",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.4") }
            }, new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail3",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.5") }
            });

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

            var newZone = new DnsZone();
            newZone.DeserializeZone(originalZone.SerializeZone());

            Assert.Equal(originalZone.Records, newZone.Records);

            return newZone.SerializeZone();
        }
    }
}
