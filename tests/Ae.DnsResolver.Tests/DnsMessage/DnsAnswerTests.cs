using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Linq;
using Xunit;

namespace Ae.DnsResolver.Tests.DnsMessage
{
    public class DnsAnswerTests
    {
        [Fact]
        public void ReadAnswer1()
        {
            int offset = 0;
            var message = SampleDnsPackets.Answer1.ReadDnsAnswer(ref offset);
            Assert.Equal(SampleDnsPackets.Answer1.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.PTR, message.Header.QueryType);
            Assert.Equal(new[] { "1", "0", "0", "127", "in-addr", "arpa" }, message.Header.Labels);
            Assert.Equal(0, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(1, message.Header.NameServerRecordCount);

            var record = message.Answers.Single();
            Assert.Equal(DnsQueryType.SOA, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal(64, record.DataOffset);
            Assert.Equal(56, record.DataLength);
            Assert.Equal(new[] { "in-addr", "arpa" }, record.Name);
            Assert.Equal(TimeSpan.Parse("10:14:32"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer2()
        {
            int offset = 0;
            var message = SampleDnsPackets.Answer2.ReadDnsAnswer(ref offset);
            Assert.Equal(SampleDnsPackets.Answer2.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(new[] { "alanedwardes-my", "sharepoint", "com" }, message.Header.Labels);
            Assert.Equal(7, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(0, message.Header.NameServerRecordCount);
            Assert.Equal(7, message.Answers.Count);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal(90, record1.DataOffset);
            Assert.Equal(15, record1.DataLength);
            Assert.Equal(new[] { "alanedwardes-my", "sharepoint", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("01:08:30"), record1.TimeToLive);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.CNAME, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal(117, record2.DataOffset);
            Assert.Equal(36, record2.DataLength);
            Assert.Equal(TimeSpan.Parse("01:08:30"), record2.TimeToLive);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.CNAME, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal(165, record3.DataOffset);
            Assert.Equal(20, record3.DataLength);
            Assert.Equal(new[] { "302-ipv4e", "clump", "dprodmgd104", "aa-rt", "sharepoint", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("02:08:00"), record3.TimeToLive);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.CNAME, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal(197, record4.DataOffset);
            Assert.Equal(63, record4.DataLength);
            Assert.Equal(new[] { "187170-ipv4e", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.TimeToLive);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.CNAME, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal(272, record5.DataOffset);
            Assert.Equal(72, record5.DataLength);
            Assert.Equal(new[] { "187170-ipv4e", "farm", "dprodmgd104", "sharepointonline", "com", "akadns", "net" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record5.TimeToLive);

            var record6 = message.Answers[5];
            Assert.Equal(DnsQueryType.CNAME, record6.Type);
            Assert.Equal(DnsQueryClass.IN, record6.Class);
            Assert.Equal(356, record6.DataOffset);
            Assert.Equal(2, record6.DataLength);
            Assert.Equal(new[] { "187170-ipv4", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com", "spo-0004", "spo-msedge", "net" }, record6.Name);
            Assert.Equal(TimeSpan.Parse("17:04:00"), record6.TimeToLive);

            var record7 = message.Answers[6];
            Assert.Equal(DnsQueryType.A, record7.Type);
            Assert.Equal(DnsQueryClass.IN, record7.Class);
            Assert.Equal(370, record7.DataOffset);
            Assert.Equal(4, record7.DataLength);
            Assert.Equal(new[] { "spo-0004", "spo-msedge", "net" }, record7.Name);
            Assert.Equal(TimeSpan.Parse("17:04:00"), record7.TimeToLive);
        }

        [Fact]
        public void ReadAnswer3()
        {
            int offset = 0;
            var message = SampleDnsPackets.Answer3.ReadDnsAnswer(ref offset);
            Assert.Equal(SampleDnsPackets.Answer3.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(1, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);

            var record = Assert.Single(message.Answers);
            Assert.Equal(DnsQueryType.A, record.Type);
            Assert.Equal(DnsQueryClass.IN, record.Class);
            Assert.Equal(50, record.DataOffset);
            Assert.Equal(4, record.DataLength);
            Assert.Equal(new[] { "google", "com" }, record.Name);
            Assert.Equal(TimeSpan.Parse("00:51:13"), record.TimeToLive);
        }

        [Fact]
        public void ReadAnswer4()
        {
            int offset = 0;
            var message = SampleDnsPackets.Answer4.ReadDnsAnswer(ref offset);
            Assert.Equal(SampleDnsPackets.Answer4.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Header.QueryClass);
            Assert.Equal(DnsQueryType.A, message.Header.QueryType);
            Assert.Equal(5, message.Header.AnswerRecordCount);
            Assert.Equal(0, message.Header.AdditionalRecordCount);
            Assert.Equal(1, message.Header.QuestionCount);
            Assert.Equal(5, message.Answers.Count);

            var record1 = message.Answers[0];
            Assert.Equal(DnsQueryType.CNAME, record1.Type);
            Assert.Equal(DnsQueryClass.IN, record1.Class);
            Assert.Equal(104, record1.DataOffset);
            Assert.Equal(2, record1.DataLength);
            Assert.Equal(new[] { "alanedwardes", "testing", "alanedwardes", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record1.TimeToLive);

            var record2 = message.Answers[1];
            Assert.Equal(DnsQueryType.A, record2.Type);
            Assert.Equal(DnsQueryClass.IN, record2.Class);
            Assert.Equal(118, record2.DataOffset);
            Assert.Equal(4, record2.DataLength);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record2.TimeToLive);

            var record3 = message.Answers[2];
            Assert.Equal(DnsQueryType.A, record3.Type);
            Assert.Equal(DnsQueryClass.IN, record3.Class);
            Assert.Equal(134, record3.DataOffset);
            Assert.Equal(4, record3.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record3.TimeToLive);

            var record4 = message.Answers[3];
            Assert.Equal(DnsQueryType.A, record4.Type);
            Assert.Equal(DnsQueryClass.IN, record4.Class);
            Assert.Equal(150, record4.DataOffset);
            Assert.Equal(4, record4.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.TimeToLive);

            var record5 = message.Answers[4];
            Assert.Equal(DnsQueryType.A, record5.Type);
            Assert.Equal(DnsQueryClass.IN, record5.Class);
            Assert.Equal(166, record5.DataOffset);
            Assert.Equal(4, record5.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record5.TimeToLive);
        }

        [Fact]
        public void ReadAnswer6()
        {
            int offset = 0;
            var message = SampleDnsPackets.Answer6.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Answer6.Length, offset);

            var bytes = message.WriteDnsHeader().ToArray();

            Assert.Equal(SampleDnsPackets.Answer6, bytes);
        }
    }
}
