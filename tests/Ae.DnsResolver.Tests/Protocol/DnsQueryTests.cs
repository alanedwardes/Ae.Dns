using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using System.Linq;
using Xunit;

namespace Ae.DnsResolver.Tests.Protocol
{
    public class DnsQueryTests
    {
        [Fact]
        public void ReadQuery1Packet()
        {
            int offset = 0;
            var header = SampleDnsPackets.Query1.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Query1.Length, offset);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal(new[] { "cognito-identity", "us-east-1", "amazonaws", "com" }, header.Labels);
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

            var bytes = header2.WriteDnsHeader().ToArray();

            Assert.Equal(SampleDnsPackets.Query1, bytes);
        }

        [Fact]
        public void ReadQuery2Packet()
        {
            int offset = 0;
            var header = SampleDnsPackets.Query2.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Query2.Length, offset);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal(new[] { "polling", "bbc", "co", "uk" }, header.Labels);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery3Packet()
        {
            int offset = 0;
            var header = SampleDnsPackets.Query3.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Query3.Length, offset);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal(new[] { "outlook", "office365", "com" }, header.Labels);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery4Packet()
        {
            int offset = 0;
            var header = SampleDnsPackets.Query4.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Query4.Length, offset);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.AAAA, header.QueryType);
            Assert.Equal(new[] { "h3", "shared", "global", "fastly", "net" }, header.Labels);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }

        [Fact]
        public void ReadQuery5Packet()
        {
            int offset = 0;
            var header = SampleDnsPackets.Query5.ReadDnsHeader(ref offset);
            Assert.Equal(SampleDnsPackets.Query5.Length, offset);

            Assert.Equal(DnsQueryClass.IN, header.QueryClass);
            Assert.Equal(DnsQueryType.A, header.QueryType);
            Assert.Equal(new[] { "roaming", "officeapps", "live", "com" }, header.Labels);
            Assert.Equal(0, header.AnswerRecordCount);
            Assert.Equal(0, header.AdditionalRecordCount);
            Assert.Equal(1, header.QuestionCount);
            Assert.Equal(0, header.NameServerRecordCount);
        }
    }
}
