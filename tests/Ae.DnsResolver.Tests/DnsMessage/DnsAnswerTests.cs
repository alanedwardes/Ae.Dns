using System;
using System.Linq;
using Xunit;

namespace Ae.DnsResolver.Tests.DnsMessage
{
    public class DnsAnswerTests
    {
        [Fact]
        public void ReadExampleExample1Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.ExampleAnswer1, ref offset);
            Assert.Equal(SampleDnsPackets.ExampleAnswer1.Length, offset);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
            Assert.Equal(new[] { "1", "0", "0", "127", "in-addr", "arpa" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(1, message.Nscount);

            var record = message.Answers.Single();
            Assert.Equal(Qtype.SOA, record.Type);
            Assert.Equal(Qclass.IN, record.Class);
            Assert.Equal(64, record.DataOffset);
            Assert.Equal(56, record.DataLength);
            Assert.Equal(new[] { "in-addr", "arpa" }, record.Name);
            Assert.Equal(TimeSpan.Parse("10:14:32"), record.Ttl);
        }

        [Fact]
        public void ReadExampleExample2Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.ExampleAnswer2, ref offset);
            Assert.Equal(SampleDnsPackets.ExampleAnswer2.Length, offset);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.A, message.Qtype);
            Assert.Equal(new[] { "alanedwardes-my", "sharepoint", "com" }, message.Labels);
            Assert.Equal(7, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Equal(7, message.Answers.Length);

            var record1 = message.Answers[0];
            Assert.Equal(Qtype.CNAME, record1.Type);
            Assert.Equal(Qclass.IN, record1.Class);
            Assert.Equal(90, record1.DataOffset);
            Assert.Equal(15, record1.DataLength);
            Assert.Equal(new[] { "alanedwardes-my", "sharepoint", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("01:08:30"), record1.Ttl);

            var record2 = message.Answers[1];
            Assert.Equal(Qtype.CNAME, record2.Type);
            Assert.Equal(Qclass.IN, record2.Class);
            Assert.Equal(117, record2.DataOffset);
            Assert.Equal(36, record2.DataLength);
            Assert.Equal(TimeSpan.Parse("01:08:30"), record2.Ttl);

            var record3 = message.Answers[2];
            Assert.Equal(Qtype.CNAME, record3.Type);
            Assert.Equal(Qclass.IN, record3.Class);
            Assert.Equal(165, record3.DataOffset);
            Assert.Equal(20, record3.DataLength);
            Assert.Equal(new[] { "302-ipv4e", "clump", "dprodmgd104", "aa-rt", "sharepoint", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("02:08:00"), record3.Ttl);

            var record4 = message.Answers[3];
            Assert.Equal(Qtype.CNAME, record4.Type);
            Assert.Equal(Qclass.IN, record4.Class);
            Assert.Equal(197, record4.DataOffset);
            Assert.Equal(63, record4.DataLength);
            Assert.Equal(new[] { "187170-ipv4e", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.Ttl);

            var record5 = message.Answers[4];
            Assert.Equal(Qtype.CNAME, record5.Type);
            Assert.Equal(Qclass.IN, record5.Class);
            Assert.Equal(272, record5.DataOffset);
            Assert.Equal(72, record5.DataLength);
            Assert.Equal(new[] { "187170-ipv4e", "farm", "dprodmgd104", "sharepointonline", "com", "akadns", "net" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record5.Ttl);

            var record6 = message.Answers[5];
            Assert.Equal(Qtype.CNAME, record6.Type);
            Assert.Equal(Qclass.IN, record6.Class);
            Assert.Equal(356, record6.DataOffset);
            Assert.Equal(2, record6.DataLength);
            Assert.Equal(new[] { "187170-ipv4", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com", "spo-0004", "spo-msedge", "net" }, record6.Name);
            Assert.Equal(TimeSpan.Parse("17:04:00"), record6.Ttl);

            var record7 = message.Answers[6];
            Assert.Equal(Qtype.A, record7.Type);
            Assert.Equal(Qclass.IN, record7.Class);
            Assert.Equal(370, record7.DataOffset);
            Assert.Equal(4, record7.DataLength);
            Assert.Equal(new[] { "spo-0004", "spo-msedge", "net" }, record7.Name);
            Assert.Equal(TimeSpan.Parse("17:04:00"), record7.Ttl);
        }

        [Fact]
        public void ReadExampleExample3Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.ExampleAnswer3, ref offset);
            Assert.Equal(SampleDnsPackets.ExampleAnswer3.Length, offset);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.A, message.Qtype);
            Assert.Equal(1, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);

            var record = Assert.Single(message.Answers);
            Assert.Equal(Qtype.A, record.Type);
            Assert.Equal(Qclass.IN, record.Class);
            Assert.Equal(50, record.DataOffset);
            Assert.Equal(4, record.DataLength);
            Assert.Equal(new[] { "google", "com" }, record.Name);
            Assert.Equal(TimeSpan.Parse("00:51:13"), record.Ttl);
        }

        [Fact]
        public void ReadExampleExample4Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.ExampleAnswer4, ref offset);
            Assert.Equal(SampleDnsPackets.ExampleAnswer4.Length, offset);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.A, message.Qtype);
            Assert.Equal(5, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(5, message.Answers.Length);

            var record1 = message.Answers[0];
            Assert.Equal(Qtype.CNAME, record1.Type);
            Assert.Equal(Qclass.IN, record1.Class);
            Assert.Equal(104, record1.DataOffset);
            Assert.Equal(2, record1.DataLength);
            Assert.Equal(new[] { "alanedwardes", "testing", "alanedwardes", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record1.Ttl);

            var record2 = message.Answers[1];
            Assert.Equal(Qtype.A, record2.Type);
            Assert.Equal(Qclass.IN, record2.Class);
            Assert.Equal(118, record2.DataOffset);
            Assert.Equal(4, record2.DataLength);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record2.Ttl);

            var record3 = message.Answers[2];
            Assert.Equal(Qtype.A, record3.Type);
            Assert.Equal(Qclass.IN, record3.Class);
            Assert.Equal(134, record3.DataOffset);
            Assert.Equal(4, record3.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record3.Ttl);

            var record4 = message.Answers[3];
            Assert.Equal(Qtype.A, record4.Type);
            Assert.Equal(Qclass.IN, record4.Class);
            Assert.Equal(150, record4.DataOffset);
            Assert.Equal(4, record4.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.Ttl);

            var record5 = message.Answers[4];
            Assert.Equal(Qtype.A, record5.Type);
            Assert.Equal(Qclass.IN, record5.Class);
            Assert.Equal(166, record5.DataOffset);
            Assert.Equal(4, record5.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record5.Ttl);
        }
    }
}
