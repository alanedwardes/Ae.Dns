using Xunit;

namespace Ae.DnsResolver.Tests
{
    public class DnsMessageReaderTests
    {
        [Fact]
        public void ReadExampleArpaPacket()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example1);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
            Assert.Equal(new[] { "1", "0", "0", "127", "in-addr", "arpa" }, message.Labels);
        }

        [Fact]
        public void ReadExampleExample1Packet()
        {
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Example2);

            Assert.Equal(Qclass.IN, message.Qclass);
            Assert.Equal(Qtype.PTR, message.Qtype);
        }
    }
}
