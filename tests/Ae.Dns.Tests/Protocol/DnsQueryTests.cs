using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsQueryTests
    {
        [Theory]
        [ClassData(typeof(QueryTheoryData))]
        public void TestReadQueries(byte[] queryBytes) => DnsByteExtensions.FromBytes<DnsMessage>(queryBytes);

        [Fact]
        public void ReadQuery1Packet()
        {
            var header = DnsByteExtensions.FromBytes<DnsHeader>(SampleDnsPackets.Query1);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("cognito-identity.us-east-1.amazonaws.com", header.Host.ToString());
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
            Assert.False(header.IsQueryResponse);
            Assert.False(header.Truncation);
            Assert.False(header.RecursionAvailable);
            Assert.True(header.RecursionDesired);
            Assert.Equal(DnsResponseCode.NoError, header.ResponseCode);

            var header2 = new DnsHeader
            {
                Id = header.Id,
                QueryClass = DnsQueryClass.IN,
                QueryType = DnsQueryType.A,
                RecursionDesired = true,
                Host = header.Host,
                QuestionCount = 1,
            };

            var bytes = DnsByteExtensions.AllocateAndWrite(header2).ToArray();

            Assert.Equal(SampleDnsPackets.Query1, bytes);
        }

        [Fact]
        public void ReadQuery2Packet()
        {
            var header = DnsByteExtensions.FromBytes<DnsHeader>(SampleDnsPackets.Query2);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("polling.bbc.co.uk", header.Host.ToString());
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery3Packet()
        {
            var header = DnsByteExtensions.FromBytes<DnsHeader>(SampleDnsPackets.Query3);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("outlook.office365.com", header.Host.ToString());
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery4Packet()
        {
            var header = DnsByteExtensions.FromBytes<DnsHeader>(SampleDnsPackets.Query4);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.AAAA, header.QueryType);
            Assert.Equal("h3.shared.global.fastly.net", header.Host.ToString());
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery5Packet()
        {
            var header = DnsByteExtensions.FromBytes<DnsHeader>(SampleDnsPackets.Query5);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("roaming.officeapps.live.com", header.Host.ToString());
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }
    }
}
