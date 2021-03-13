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
        public void TestReadAnswers(byte[] answerBytes) => DnsByteExtensions.FromBytes<DnsAnswer>(answerBytes);

        [Fact]
        public void ReadAnswer1()
        {
            var message = DnsByteExtensions.FromBytes<DnsAnswer>(SampleDnsPackets.Answer1);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, message.Header.QueryType);
            Assert.Equal("1.0.0.127.in-addr.arpa", message.Header.Host);
            Assert.Equal(0, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(1, message.Header.NameServerRecordCount);

            var record = message.Answers.Single();
            Assert.Equal(DnsQueryType.SOA, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("in-addr.arpa", record.Host);

            var soaData = (DnsSoaResource)record.Resource;
            Assert.Equal("b.in-addr-servers.arpa", soaData.MName);
            Assert.Equal("nstld.iana.org", soaData.RName);
            Assert.Equal(TimeSpan.Parse("00:36:32"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer2()
        {
            var message = DnsByteExtensions.FromBytes<DnsAnswer>(SampleDnsPackets.Answer2);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal("alanedwardes-my.sharepoint.com", message.Header.Host);
            Assert.Equal(7, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(0, message.Header.NameServerRecordCount);
            Assert.Equal(7, message.Answers.Count);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes-my.sharepoint.com", record1.Host);
            Assert.Equal(TimeSpan.Parse("01:00:00"), record1.TimeToLive);
            Assert.Equal("alanedwardes.sharepoint.com", ((DnsTextResource)record1.Resource).Text);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.CNAME, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal("alanedwardes.sharepoint.com", record2.Host);
            Assert.Equal(TimeSpan.Parse("01:00:00"), record2.TimeToLive);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", ((DnsTextResource)record2.Resource).Text);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.CNAME, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", record3.Host);
            Assert.Equal(TimeSpan.Parse("00:00:30"), record3.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", ((DnsTextResource)record3.Resource).Text);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.CNAME, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", record4.Host);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record4.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", ((DnsTextResource)record4.Resource).Text);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.CNAME, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", record5.Host);
            Assert.Equal(TimeSpan.Parse("00:05:00"), record5.TimeToLive);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", ((DnsTextResource)record5.Resource).Text);

            var record6 = message.Answers[5];
            Assert.Equal(DnsQueryType.CNAME, record6.Type);
            Assert.Equal(DnsQueryClass.IN, record6.Class);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", record6.Host);
            Assert.Equal(TimeSpan.Parse("00:04:00"), record6.TimeToLive);

            var record7 = message.Answers[6];
            Assert.Equal(DnsQueryType.A, record7.Type);
            Assert.Equal(DnsQueryClass.IN, record7.Class);
            Assert.Equal("spo-0004.spo-msedge.net", record7.Host);
            Assert.Equal(IPAddress.Parse("13.107.136.9"), ((DnsIpAddressResource)record7.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:04:00"), record7.TimeToLive);
        }

        [Fact]
        public void ReadAnswer3()
        {
            var message = DnsByteExtensions.FromBytes<DnsAnswer>(SampleDnsPackets.Answer3);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(1, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);

            var record = Assert.Single(message.Answers);
            Assert.Equal(DnsQueryType.A, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("google.com", record.Host);
            Assert.Equal(IPAddress.Parse("216.58.210.206"), ((DnsIpAddressResource)record.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:04:28"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer4()
        {
            var message = DnsByteExtensions.FromBytes<DnsAnswer>(SampleDnsPackets.Answer4);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(5, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(5, message.Answers.Count);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes.testing.alanedwardes.com", record1.Host);
            Assert.Equal("alanedwardes.com", ((DnsTextResource)record1.Resource).Text);
            Assert.Equal(TimeSpan.Parse("00:05:00"), record1.TimeToLive);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.A, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal(IPAddress.Parse("143.204.191.46"), ((DnsIpAddressResource)record2.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record2.TimeToLive);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.A, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("alanedwardes.com", record3.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.37"), ((DnsIpAddressResource)record3.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record3.TimeToLive);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.A, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("alanedwardes.com", record4.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.71"), ((DnsIpAddressResource)record4.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record4.TimeToLive);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.A, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("alanedwardes.com", record5.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.110"), ((DnsIpAddressResource)record5.Resource).IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record5.TimeToLive);
        }
    }
}
