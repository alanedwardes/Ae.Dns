using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsHeaderTests
    {
        [Fact]
        public void TestDnsHeaderIsQueryResponse()
        {
            foreach (var answer in SampleDnsPackets.Answers)
            {
                Assert.True(DnsByteExtensions.FromBytes<DnsMessage>(answer).Header.IsQueryResponse);
            }

            foreach (var query in SampleDnsPackets.Queries)
            {
                Assert.False(DnsByteExtensions.FromBytes<DnsMessage>(query).Header.IsQueryResponse);
            }
        }

        [Fact]
        public void TestDnsHeaderGetSetFlags()
        {
            var header = new DnsHeader();
            header.Flags = 0;

            header.OperationCode = DnsOperationCode.IQUERY;
            Assert.Equal(DnsOperationCode.IQUERY, header.OperationCode);
            Assert.Equal(2048, header.Flags);

            header.ResponseCode = DnsResponseCode.NotImp;
            Assert.Equal(DnsResponseCode.NotImp, header.ResponseCode);
            Assert.Equal(2052, header.Flags);

            header.IsQueryResponse = true;
            Assert.True(header.IsQueryResponse);
            Assert.Equal(34820, header.Flags);

            header.AuthoritativeAnswer = true;
            Assert.True(header.AuthoritativeAnswer);
            Assert.Equal(35844, header.Flags);

            header.RecursionAvailable = true;
            Assert.True(header.RecursionAvailable);
            Assert.Equal(35972, header.Flags);

            header.RecusionDesired = true;
            Assert.True(header.RecusionDesired);
            Assert.Equal(36228, header.Flags);
        }
    }
}
