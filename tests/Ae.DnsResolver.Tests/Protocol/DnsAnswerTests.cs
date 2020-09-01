using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using Ae.DnsResolver.Protocol.Records;
using System;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.DnsResolver.Tests.Protocol
{
    public class DnsAnswerTests
    {
        [Theory]
        [ClassData(typeof(AnswerTheoryData))]
        public void TestReadAnswers(byte[] answerBytes) => answerBytes.ReadDnsAnswer();

        [Fact]
        public void ReadAnswer1()
        {
            var message = SampleDnsPackets.Answer1.ReadDnsAnswer();

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, message.Header.QueryType);
            Assert.Equal("1.0.0.127.in-addr.arpa", message.Header.Host);
            Assert.Equal(0, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(1, message.Header.NameServerRecordCount);

            var record = (DnsSoaRecord)message.Answers.Single();
            Assert.Equal(DnsQueryType.SOA, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("in-addr.arpa", record.Host);
            Assert.Equal("b.in-addr-servers.arpa", record.MName);
            Assert.Equal("nstld.iana.org", record.RName);
            Assert.Equal(TimeSpan.Parse("00:36:32"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer2()
        {
            var message = SampleDnsPackets.Answer2.ReadDnsAnswer();

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal("alanedwardes-my.sharepoint.com", message.Header.Host);
            Assert.Equal(7, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(0, message.Header.NameServerRecordCount);
            Assert.Equal(7, message.Answers.Count);

            var record1 = (DnsTextRecord)message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes-my.sharepoint.com", record1.Host);
            Assert.Equal(TimeSpan.Parse("01:00:00"), record1.TimeToLive);
            Assert.Equal("alanedwardes.sharepoint.com", record1.Text);

            var record2 = (DnsTextRecord)message.Answers[1];
            Assert.Equal(DnsQueryType.CNAME, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal("alanedwardes.sharepoint.com", record2.Host);
            Assert.Equal(TimeSpan.Parse("01:00:00"), record2.TimeToLive);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", record2.Text);

            var record3 = (DnsTextRecord)message.Answers[2];
            Assert.Equal(DnsQueryType.CNAME, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("302-ipv4e.clump.dprodmgd104.aa-rt.sharepoint.com", record3.Host);
            Assert.Equal(TimeSpan.Parse("00:00:30"), record3.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", record3.Text);

            var record4 = (DnsTextRecord)message.Answers[3];
            Assert.Equal(DnsQueryType.CNAME, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.aa-rt.sharepoint.com", record4.Host);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record4.TimeToLive);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", record4.Text);

            var record5 = (DnsTextRecord)message.Answers[4];
            Assert.Equal(DnsQueryType.CNAME, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("187170-ipv4e.farm.dprodmgd104.sharepointonline.com.akadns.net", record5.Host);
            Assert.Equal(TimeSpan.Parse("00:05:00"), record5.TimeToLive);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", record5.Text);

            var record6 = (DnsTextRecord)message.Answers[5];
            Assert.Equal(DnsQueryType.CNAME, record6.Type);
            Assert.Equal(DnsQueryClass.IN, record6.Class);
            Assert.Equal("187170-ipv4.farm.dprodmgd104.aa-rt.sharepoint.com.spo-0004.spo-msedge.net", record6.Host);
            Assert.Equal(TimeSpan.Parse("00:04:00"), record6.TimeToLive);

            var record7 = (DnsIpAddressRecord)message.Answers[6];
            Assert.Equal(DnsQueryType.A, record7.Type);
            Assert.Equal(DnsQueryClass.IN, record7.Class);
            Assert.Equal("spo-0004.spo-msedge.net", record7.Host);
            Assert.Equal(IPAddress.Parse("13.107.136.9"), record7.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:04:00"), record7.TimeToLive);
        }

        [Fact]
        public void ReadAnswer3()
        {
            var message = SampleDnsPackets.Answer3.ReadDnsAnswer();

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(1, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);

            var record = (DnsIpAddressRecord)Assert.Single(message.Answers);
            Assert.Equal(DnsQueryType.A, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal("google.com", record.Host);
            Assert.Equal(IPAddress.Parse("216.58.210.206"), record.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:04:28"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer4()
        {
            var message = SampleDnsPackets.Answer4.ReadDnsAnswer();

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(5, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(5, message.Answers.Count);

            var record1 = (DnsTextRecord)message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal("alanedwardes.testing.alanedwardes.com", record1.Host);
            Assert.Equal("alanedwardes.com", record1.Text);
            Assert.Equal(TimeSpan.Parse("00:05:00"), record1.TimeToLive);

            var record2 = (DnsIpAddressRecord)message.Answers[1];
            Assert.Equal(DnsQueryType.A, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal(IPAddress.Parse("143.204.191.46"), record2.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record2.TimeToLive);

            var record3 = (DnsIpAddressRecord)message.Answers[2];
            Assert.Equal(DnsQueryType.A, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal("alanedwardes.com", record3.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.37"), record3.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record3.TimeToLive);

            var record4 = (DnsIpAddressRecord)message.Answers[3];
            Assert.Equal(DnsQueryType.A, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal("alanedwardes.com", record4.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.71"), record4.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record4.TimeToLive);

            var record5 = (DnsIpAddressRecord)message.Answers[4];
            Assert.Equal(DnsQueryType.A, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal("alanedwardes.com", record5.Host);
            Assert.Equal(IPAddress.Parse("143.204.191.110"), record5.IPAddress);
            Assert.Equal(TimeSpan.Parse("00:01:00"), record5.TimeToLive);
        }
    }
}
