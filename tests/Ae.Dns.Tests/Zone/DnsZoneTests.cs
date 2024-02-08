using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace Ae.Dns.Tests.Zone
{
    [Obsolete]
    public sealed class DnsZoneTests
    {
        [Fact]
        public void TestRoundTripZone()
        {
            var originalZone = new DnsZone();

            originalZone.Origin = "example.com";
            originalZone.DefaultTtl = TimeSpan.FromSeconds(3600);

            originalZone.Records.Add(new DnsResourceRecord
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
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.NS,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "ns" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.NS,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "ns.somewhere.example" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 10, Entries = "mail.example.com" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 20, Entries = "mail2.example.com" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.MX,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsMxResource { Preference = 50, Entries = "mail3" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.1") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.AAAA,
                Host = "example.com",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("2001:db8:10::1") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "ns",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.2") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.AAAA,
                Host = "ns",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("2001:db8:10::2") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "www",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "example.com" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.CNAME,
                Host = "wwwtest",
                TimeToLive = 3600,
                Resource = new DnsDomainResource { Entries = "www" }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.3") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail2",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.4") }
            });

            originalZone.Records.Add(new DnsResourceRecord
            {
                Class = DnsQueryClass.IN,
                Type = DnsQueryType.A,
                Host = "mail3",
                TimeToLive = 3600,
                Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.0.2.5") }
            });

            var stream = new MemoryStream();
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 4096, true))
            {
                originalZone.SerializeZone(sw);
            }

            var clonedZone = new DnsZone();

            stream.Position = 0;
            using (var sr = new StreamReader(stream, Encoding.UTF8, true, 4096))
            {
                clonedZone.DeserializeZone(sr);
            }

            Assert.Equal(originalZone.Records, clonedZone.Records);
        }
    }
}
