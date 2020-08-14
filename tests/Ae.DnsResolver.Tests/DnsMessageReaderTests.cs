using System;
using System.Net;
using Xunit;

namespace Ae.DnsResolver.Tests
{
    public class DnsMessageReaderTests
    {
        [Fact]
        public void ReadExampleExample1Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example1);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
            Assert.Equal(new[] { "1", "0", "0", "127", "in-addr", "arpa" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Empty(message.Records);
        }

        [Fact]
        public void ReadExampleExample2Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example2);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
        }

        [Fact]
        public void ReadExampleExample3Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example3);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.A, message.Qtype);
            Assert.Equal(1, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);

            var record = Assert.Single(message.Records);
            Assert.Equal(Qtype.A, record.Type);
            Assert.Equal(Qclass.IN, record.Class);
            Assert.Equal(50, record.DataOffset);
            Assert.Equal(4, record.DataLength);
            Assert.Equal(new[] { "google", "com" }, record.Name);
            Assert.Equal(TimeSpan.Parse("00:51:13"), record.Ttl);
        }

        [Theory]
        [InlineData(4, 104, new[] { "alanedwardes", "com" })]
        public void ReadCnameRecordTests(int example, int offset, string[] expected)
        {
            var value = EndianExtensions.ReadString(SampleDnsPackets.Examples[example], ref offset);
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(3, 50, new byte[] { 216, 58, 210, 206 })]
        [InlineData(4, 118, new byte[] { 143, 204, 191, 46 })]
        [InlineData(4, 134, new byte[] { 143, 204, 191, 37 })]
        [InlineData(4, 150, new byte[] { 143, 204, 191, 71 })]
        [InlineData(4, 166, new byte[] { 143, 204, 191, 110 })]
        public void ReadARecordTests(int example, int offset, byte[] expected)
        {
            var value = SampleDnsPackets.Examples[example].ReadBytes(4, ref offset);
            Assert.Equal(expected, value);
        }

        [Fact]
        public void ReadExampleExample4Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example4);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.A, message.Qtype);
            Assert.Equal(5, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(5, message.Records.Length);

            var record1 = message.Records[0];
            Assert.Equal(Qtype.CNAME, record1.Type);
            Assert.Equal(Qclass.IN, record1.Class);
            Assert.Equal(104, record1.DataOffset);
            Assert.Equal(2, record1.DataLength);
            Assert.Equal(new[] { "alanedwardes", "testing", "alanedwardes", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record1.Ttl);

            var record2 = message.Records[1];
            Assert.Equal(Qtype.A, record2.Type);
            Assert.Equal(Qclass.IN, record2.Class);
            Assert.Equal(118, record2.DataOffset);
            Assert.Equal(4, record2.DataLength);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record2.Ttl);

            var record3 = message.Records[2];
            Assert.Equal(Qtype.A, record3.Type);
            Assert.Equal(Qclass.IN, record3.Class);
            Assert.Equal(134, record3.DataOffset);
            Assert.Equal(4, record3.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record3.Ttl);

            var record4 = message.Records[3];
            Assert.Equal(Qtype.A, record4.Type);
            Assert.Equal(Qclass.IN, record4.Class);
            Assert.Equal(150, record4.DataOffset);
            Assert.Equal(4, record4.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.Ttl);

            var record5 = message.Records[4];
            Assert.Equal(Qtype.A, record5.Type);
            Assert.Equal(Qclass.IN, record5.Class);
            Assert.Equal(166, record5.DataOffset);
            Assert.Equal(4, record5.DataLength);
            Assert.Equal(new[] { "alanedwardes", "com" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record5.Ttl);
        }
    }
}
