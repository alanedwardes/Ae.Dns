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
                var header = answer.FromBytes<DnsHeader>();
                Assert.True(header.IsQueryResponse);
            }

            foreach (var query in SampleDnsPackets.Queries)
            {
                var header = query.FromBytes<DnsHeader>();
                Assert.False(header.IsQueryResponse);
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

        [Fact]
        public void TestDnsHeaderIsNxDomainResponse()
        {
            var header = SampleDnsPackets.Answer1.FromBytes<DnsHeader>();
            Assert.Equal(DnsResponseCode.NXDomain, header.ResponseCode);
        }
    }
}
