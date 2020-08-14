using Ae.DnsResolver.Client;
using Xunit;

namespace Ae.DnsResolver.Tests.DnsMessage
{
    public class DnsQueryTests
    {
        [Fact]
        public void ReadQuery1Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query1, ref offset);
            Assert.Equal(SampleDnsPackets.Query1.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Qclass);
            Assert.Equal(DnsQueryType.A, message.Qtype);
            Assert.Equal(new[] { "cognito-identity", "us-east-1", "amazonaws", "com" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery2Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query2, ref offset);
            Assert.Equal(SampleDnsPackets.Query2.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Qclass);
            Assert.Equal(DnsQueryType.A, message.Qtype);
            Assert.Equal(new[] { "polling", "bbc", "co", "uk" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery3Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query3, ref offset);
            Assert.Equal(SampleDnsPackets.Query3.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Qclass);
            Assert.Equal(DnsQueryType.A, message.Qtype);
            Assert.Equal(new[] { "outlook", "office365", "com" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery4Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query4, ref offset);
            Assert.Equal(SampleDnsPackets.Query4.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Qclass);
            Assert.Equal(DnsQueryType.AAAA, message.Qtype);
            Assert.Equal(new[] { "h3", "shared", "global", "fastly", "net" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery5Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query5, ref offset);
            Assert.Equal(SampleDnsPackets.Query5.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.Qclass);
            Assert.Equal(DnsQueryType.A, message.Qtype);
            Assert.Equal(new[] { "roaming", "officeapps", "live", "com" }, message.Labels);
            Assert.Equal(0, message.Ancount);
            Assert.Equal(0, message.Arcount);
            Assert.Equal(1, message.Qdcount);
            Assert.Equal(0, message.Nscount);
            Assert.Empty(message.Questions);
        }
    }
}
