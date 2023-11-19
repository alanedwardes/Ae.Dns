using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsAnswerTests
    {
        [Theory]
        [ClassData(typeof(AnswerTheoryData))]
        public void TestReadAnswers(byte[] answerBytes) => DnsByteExtensions.FromBytes<DnsMessage>(answerBytes);

        [Fact]
        public void ReadAnswer1()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer1);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, message.Header.QueryType);
            Assert.Equal("1.0.0.127.in-addr.arpa", message.Header.Host);
            Assert.Equal(0, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(1, message.Header.NameServerRecordCount);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Answers);

            var record = message.Nameservers.Single();
            Assert.Equal(DnsQueryType.SOA, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("in-addr.arpa", record.Host);

            var soaData = (DnsSoaResource)record.Resource;
            Assert.Equal("b.in-addr-servers.arpa", string.Join(".", soaData.MName));
            Assert.Equal("nstld.iana.org", string.Join(".", soaData.RName));
            Assert.Equal((uint)TimeSpan.Parse("00:36:32").TotalSeconds, record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer2()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer2);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal("alanedwardes-my.sharepoint.com", message.Header.Host);
            Assert.Equal(7, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(0, message.Header.NameServerRecordCount);
            Assert.Equal(7, message.Answers.Count);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Nameservers);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes-my.sharepoint.com", record1.Host);
            Assert.Equal((uint)TimeSpan.Parse("01:00:00").TotalSeconds, record1.TimeToLive);
            Assert.Equal("alanedwardes.sharepoint.com", ((DnsDomainResource)record1.Resource).Domain);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.CNAME, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal("alanedwardes.sharepoint.com", record2.Host);
            Assert.Equal((uint)TimeSpan.Parse("01:00:00").TotalSeconds, record2.TimeToLive);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", ((DnsDomainResource)record2.Resource).Domain);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.CNAME, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", record3.Host);
            Assert.Equal((uint)TimeSpan.Parse("00:00:30").TotalSeconds, record3.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", ((DnsDomainResource)record3.Resource).Domain);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.CNAME, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", record4.Host);
            Assert.Equal((uint)TimeSpan.Parse("00:01:00").TotalSeconds, record4.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", ((DnsDomainResource)record4.Resource).Domain);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.CNAME, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", record5.Host);
            Assert.Equal((uint)TimeSpan.Parse("00:05:00").TotalSeconds, record5.TimeToLive);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", ((DnsDomainResource)record5.Resource).Domain);

            var record6 = message.Answers[5];
            Assert.Equal(DnsQueryType.CNAME, record6.Type);
            Assert.Equal(DnsQueryClass.IN, record6.Class);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", record6.Host);
            Assert.Equal((uint)TimeSpan.Parse("00:04:00").TotalSeconds, record6.TimeToLive);

            var record7 = message.Answers[6];
            Assert.Equal(DnsQueryType.A, record7.Type);
            Assert.Equal(DnsQueryClass.IN, record7.Class);
            Assert.Equal("spo-0004.spo-msedge.net", record7.Host);
            Assert.Equal(IPAddress.Parse("13.107.136.9"), ((DnsIpAddressResource)record7.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:04:00").TotalSeconds, record7.TimeToLive);
        }

        [Fact]
        public void ReadAnswer3()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer3);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(1, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Nameservers);

            var record = Assert.Single(message.Answers);
            Assert.Equal(DnsQueryType.A, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("google.com", record.Host);
            Assert.Equal(IPAddress.Parse("216.58.210.206"), ((DnsIpAddressResource)record.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:04:28").TotalSeconds, record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer4()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer4);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(5, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(5, message.Answers.Count);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Nameservers);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes.testing.alanedwardes.com", record1.Host);
            Assert.Equal("alanedwardes.com", ((DnsDomainResource)record1.Resource).Domain);
            Assert.Equal((uint)TimeSpan.Parse("00:05:00").TotalSeconds, record1.TimeToLive);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.A, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal(IPAddress.Parse("143.204.191.46"), ((DnsIpAddressResource)record2.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:01:00").TotalSeconds, record2.TimeToLive);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.A, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("alanedwardes.com", record3.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.37"), ((DnsIpAddressResource)record3.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:01:00").TotalSeconds, record3.TimeToLive);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.A, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("alanedwardes.com", record4.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.71"), ((DnsIpAddressResource)record4.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:01:00").TotalSeconds, record4.TimeToLive);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.A, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("alanedwardes.com", record5.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.110"), ((DnsIpAddressResource)record5.Resource).IPAddress);
            Assert.Equal((uint)TimeSpan.Parse("00:01:00").TotalSeconds, record5.TimeToLive);
        }

        [Fact]
        public void ReadAnswer10()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer10);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.TEXT, message.Header.QueryType);
            Assert.Equal(1, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(1, message.Answers.Count);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Nameservers);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.TEXT, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("_spf.mailgun.org", record1.Host);

            var entries = ((DnsTextResource)record1.Resource).Entries;
            Assert.Equal(2, entries.Length);
            Assert.Equal("v=spf1 ip4:209.61.151.0/24 ip4:166.78.68.0/22 ip4:198.61.254.0/23 ip4:192.237.158.0/23 ip4:23.253.182.0/23 ip4:104.130.96.0/28 ip4:146.20.113.0/24 ip4:146.20.191.0/24 ip4:159.135.224.0/20 ip4:69.72.32.0/20", entries[0]);
            Assert.Equal(" ip4:104.130.122.0/23 ip4:146.20.112.0/26 ip4:161.38.192.0/20 ip4:143.55.224.0/21 ip4:143.55.232.0/22 ip4:159.112.240.0/20 ~all", entries[1]);
            Assert.Equal((uint)TimeSpan.Parse("00:00:21").TotalSeconds, record1.TimeToLive);
        }

        [Fact]
        public void ReadAnswer11()
        {
            var message = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer11);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.NS, message.Header.QueryType);
            Assert.Equal(4, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(4, message.Answers.Count);

            Assert.Empty(message.Additional);
            Assert.Empty(message.Nameservers);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.NS, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("google.com", record1.Host);
            Assert.Equal("ns4.google.com", ((DnsDomainResource)record1.Resource).Domain);
            Assert.Equal(21242u, record1.TimeToLive);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.NS, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal("google.com", record2.Host);
            Assert.Equal("ns3.google.com", ((DnsDomainResource)record2.Resource).Domain);
            Assert.Equal(21242u, record2.TimeToLive);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.NS, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("google.com", record3.Host);
            Assert.Equal("ns1.google.com", ((DnsDomainResource)record3.Resource).Domain);
            Assert.Equal(21242u, record3.TimeToLive);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.NS, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("google.com", record4.Host);
            Assert.Equal("ns2.google.com", ((DnsDomainResource)record4.Resource).Domain);
            Assert.Equal(21242u, record4.TimeToLive);
        }

        [Fact]
        public void ReadAnswer12()
        {
            var value = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer12);

            Assert.Equal("MS=6BF03E6AF5CB689E315FB6199603BABF2C88D805", Assert.Single(((DnsTextResource)value.Answers[0].Resource).Entries));
            Assert.Equal("MS=ms44452932", Assert.Single(((DnsTextResource)value.Answers[1].Resource).Entries));
            Assert.Equal("MS=ms58704441", Assert.Single(((DnsTextResource)value.Answers[2].Resource).Entries));
            Assert.Equal("adobe-idp-site-verification=b92c9e999aef825edc36e0a3d847d2dbad5b2fc0e05c79ddd7a16139b48ecf4b", Assert.Single(((DnsTextResource)value.Answers[3].Resource).Entries));
            Assert.Equal("apple-domain-verification=RyQhdzTl6Z6x8ZP4", Assert.Single(((DnsTextResource)value.Answers[4].Resource).Entries));
            Assert.Equal("atlassian-domain-verification=jjgw98AKv2aeoYFxiL/VFaoyPkn3undEssTRuMg6C/3Fp/iqhkV4HVV7WjYlVeF8", Assert.Single(((DnsTextResource)value.Answers[5].Resource).Entries));
            Assert.Equal("docusign=087098e3-3d46-47b7-9b4e-8a23028154cd", Assert.Single(((DnsTextResource)value.Answers[6].Resource).Entries));
            Assert.Equal("google-site-verification=UTM-3akMgubp6tQtgEuAkYNYLyYAvpTnnSrDMWoDR3o", Assert.Single(((DnsTextResource)value.Answers[7].Resource).Entries));
            Assert.Equal("stripe-verification=f88ef17321660a01bab1660454192e014defa29ba7b8de9633c69d6b4912217f", Assert.Single(((DnsTextResource)value.Answers[8].Resource).Entries));
            Assert.Equal("v=spf1 ip4:192.30.252.0/22 include:_netblocks.google.com include:_netblocks2.google.com include:_netblocks3.google.com include:spf.protection.outlook.com include:mail.zendesk.com include:_spf.salesforce.com include:servers.mcsv.net ip4:166.78.69.169 ip4:1", ((DnsTextResource)value.Answers[9].Resource).Entries[0]);
            Assert.Equal("66.78.69.170 ip4:166.78.71.131 ip4:167.89.101.2 ip4:167.89.101.192/28 ip4:192.254.112.60 ip4:192.254.112.98/31 ip4:192.254.113.10 ip4:192.254.113.101 ip4:192.254.114.176 ip4:62.253.227.114 ~all", ((DnsTextResource)value.Answers[9].Resource).Entries[1]);
        }

        [Fact(Skip = "Skipped, it is unclear whether this is a valid packet")]
        public void ReadAnswer13()
        {
            var value = DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer13);

            var answers = value.Answers.Select(x => x.Resource).Cast<DnsStringResource>().Select(x => x.Entries.Single()).ToArray();

            Assert.Equal(29, answers.Length);

            Assert.Equal("Nam eu nisl mauris. Maecenas at risus sit amet enim vestibulum efficitur sit amet quis quam.", answers[0]);
            Assert.Equal("Sed sodales nisi vitae nisi eleifend, vitae vestibulum nunc tempor. Integer vitae mauris odio.", answers[1]);
            Assert.Equal("Sed imperdiet tellus sit amet nibh tempus, at tincidunt purus vestibulum. Duis ut sagittis diam.", answers[2]);
            Assert.Equal("Sed dapibus placerat malesuada. Aenean accumsan arcu in nisl laoreet consectetur. Nunc quis gravida dolor.", answers[3]);
            Assert.Equal("In ultrices mi nec mauris volutpat tincidunt. Fusce nisl ligula, venenatis non elit sit amet, pretium semper quam.", answers[4]);
            Assert.Equal("Donec vel sapien sed tortor mollis interdum. Suspendisse tortor nisl, molestie eget nunc eget, tempor tincidunt diam.", answers[5]);
            Assert.Equal("Cras sed nulla nibh. Morbi convallis venenatis purus ut condimentum. Fusce enim velit, cursus ut elementum id, dignissim vitae lacus.", answers[6]);
            Assert.Equal("Pellentesque porttitor scelerisque nisi, ut dapibus velit rutrum volutpat. Nullam ultrices pellentesque massa, in suscipit velit porta id.", answers[7]);
            Assert.Equal("Vivamus at massa vel magna viverra sodales sit amet a velit. Vivamus ut lorem sed dui tincidunt fermentum ut a leo. In pretium feugiat pretium.", answers[8]);
            Assert.Equal("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent quis sagittis est. Donec imperdiet nunc lacinia quam eleifend tincidunt. Duis vel arcu orci.", answers[9]);
            Assert.Equal("Vivamus euismod felis nulla, eu congue orci feugiat ac. Curabitur posuere tortor eget pellentesque luctus. Integer non mi sit amet urna ullamcorper efficitur.", answers[10]);
            Assert.Equal("Ut dapibus magna nec ante euismod, ut ornare justo sagittis. Vestibulum at elit id arcu convallis vehicula. Sed ac odio non augue ornare tempus sit amet vitae ligula.", answers[11]);
            Assert.Equal("Suspendisse ornare porttitor massa ac tempor. Fusce eu cursus quam. Aliquam elit libero, dapibus eu accumsan et, accumsan sit amet ipsum. Ut consectetur et odio ut tempor.", answers[12]);
        }
    }
}
