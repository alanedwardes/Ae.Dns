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
            Assert.Equal(Qtype.PTR, message.Qtype);
        }

        [Fact]
        public void ReadExampleExample4Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example4);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
        }
    }
}
