using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Linq;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsQueryTests
    {
        [Theory]
        [ClassData(typeof(QueryTheoryData))]
        public void TestReadQueries(byte[] queryBytes) => queryBytes.ReadDnsHeader();

        [Fact]
        public void ReadQuery1Packet()
        {
            var header = SampleDnsPackets.Query1.ReadDnsHeader();

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("cognito-identity.us-east-1.amazonaws.com", header.Host);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
            Assert.False(header.IsQueryResponse);
            Assert.False(header.Truncation);
            Assert.False(header.RecursionAvailable);
            Assert.True(header.RecusionDesired);
            Assert.Equal(DnsResponseCode.NoError, header.ResponseCode);

            var header2 = new DnsHeader
            {
                Id = header.Id,
                QueryClass = DnsQueryClass.IN,
                QueryType = DnsQueryType.A,
                RecusionDesired = true,
                Labels = header.Labels,
                QuestionCount = 1,
            };

            var bytes = header2.ToBytes().ToArray();

            Assert.Equal(SampleDnsPackets.Query1, bytes);
        }

        [Fact]
        public void ReadQuery2Packet()
        {
            var header = SampleDnsPackets.Query2.ReadDnsHeader();

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("polling.bbc.co.uk", header.Host);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery3Packet()
        {
            var header = SampleDnsPackets.Query3.ReadDnsHeader();

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("outlook.office365.com", header.Host);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery4Packet()
        {
            var header = SampleDnsPackets.Query4.ReadDnsHeader();

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.AAAA, header.QueryType);
            Assert.Equal("h3.shared.global.fastly.net", header.Host);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery5Packet()
        {
            var header = SampleDnsPackets.Query5.ReadDnsHeader();

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal("roaming.officeapps.live.com", header.Host);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }
    }
}
