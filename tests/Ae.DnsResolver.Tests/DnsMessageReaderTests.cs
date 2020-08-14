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
            var offset = record.DataOffset;
            Assert.Equal(IPAddress.Parse("216.58.210.206"), new IPAddress(SampleDnsPackets.Example3.ReadBytes(record.DataLength, ref offset)));
            Assert.Equal(new[] { "google", "com" }, record.Name);
            Assert.Equal(TimeSpan.Parse("00:51:13"), record.Ttl);
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
            
            Assert.Equal(new[] { "alanedwardes", "testing", "alanedwardes", "com" }, record1.Name);
            Assert.Equal(TimeSpan.Parse("03:07:45"), record1.Ttl);

            var offset1 = record1.DataOffset;
            var value = EndianExtensions.ReadString(SampleDnsPackets.Example4, ref offset1);
            Assert.Equal(new[] { "alanedwardes", "com" }, value);

            var record2 = message.Records[1];
            Assert.Equal(Qtype.A, record2.Type);
            Assert.Equal(Qclass.IN, record2.Class);
            var offset2 = record2.DataOffset;
            Assert.Equal(IPAddress.Parse("143.204.191.46"), new IPAddress(SampleDnsPackets.Example4.ReadBytes(record2.DataLength, ref offset2)));
            Assert.Equal(new[] { "alanedwardes", "com" }, record2.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record2.Ttl);

            var record3 = message.Records[2];
            Assert.Equal(Qtype.A, record3.Type);
            Assert.Equal(Qclass.IN, record3.Class);
            var offset3 = record3.DataOffset;
            Assert.Equal(IPAddress.Parse("143.204.191.37"), new IPAddress(SampleDnsPackets.Example4.ReadBytes(record3.DataLength, ref offset3)));
            Assert.Equal(new[] { "alanedwardes", "com" }, record3.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record3.Ttl);

            var record4 = message.Records[3];
            Assert.Equal(Qtype.A, record4.Type);
            Assert.Equal(Qclass.IN, record4.Class);
            var offset4 = record4.DataOffset;
            Assert.Equal(IPAddress.Parse("143.204.191.71"), new IPAddress(SampleDnsPackets.Example4.ReadBytes(record4.DataLength, ref offset4)));
            Assert.Equal(new[] { "alanedwardes", "com" }, record4.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record4.Ttl);

            var record5 = message.Records[4];
            Assert.Equal(Qtype.A, record5.Type);
            Assert.Equal(Qclass.IN, record5.Class);
            var offset5 = record5.DataOffset;
            Assert.Equal(IPAddress.Parse("143.204.191.110"), new IPAddress(SampleDnsPackets.Example4.ReadBytes(record5.DataLength, ref offset5)));
            Assert.Equal(new[] { "alanedwardes", "com" }, record5.Name);
            Assert.Equal(TimeSpan.Parse("04:16:00"), record5.Ttl);
        }
    }
}
