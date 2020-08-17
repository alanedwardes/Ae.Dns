using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
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

            Assert.Equal(DnsQueryClass.IN, message.QueryClass);
            Assert.Equal(DnsQueryType.A, message.QueryType);
            Assert.Equal(new[] { "cognito-identity", "us-east-1", "amazonaws", "com" }, message.Labels);
            Assert.Equal(0, message.AnswerRecordCount);
            Assert.Equal(0, message.AdditionalRecordCount);
            Assert.Equal(1, message.QuestionCount);
            Assert.Equal(0, message.NameServerRecordCount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery2Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query2, ref offset);
            Assert.Equal(SampleDnsPackets.Query2.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.QueryClass);
            Assert.Equal(DnsQueryType.A, message.QueryType);
            Assert.Equal(new[] { "polling", "bbc", "co", "uk" }, message.Labels);
            Assert.Equal(0, message.AnswerRecordCount);
            Assert.Equal(0, message.AdditionalRecordCount);
            Assert.Equal(1, message.QuestionCount);
            Assert.Equal(0, message.NameServerRecordCount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery3Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query3, ref offset);
            Assert.Equal(SampleDnsPackets.Query3.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.QueryClass);
            Assert.Equal(DnsQueryType.A, message.QueryType);
            Assert.Equal(new[] { "outlook", "office365", "com" }, message.Labels);
            Assert.Equal(0, message.AnswerRecordCount);
            Assert.Equal(0, message.AdditionalRecordCount);
            Assert.Equal(1, message.QuestionCount);
            Assert.Equal(0, message.NameServerRecordCount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery4Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query4, ref offset);
            Assert.Equal(SampleDnsPackets.Query4.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.QueryClass);
            Assert.Equal(DnsQueryType.AAAA, message.QueryType);
            Assert.Equal(new[] { "h3", "shared", "global", "fastly", "net" }, message.Labels);
            Assert.Equal(0, message.AnswerRecordCount);
            Assert.Equal(0, message.AdditionalRecordCount);
            Assert.Equal(1, message.QuestionCount);
            Assert.Equal(0, message.NameServerRecordCount);
            Assert.Empty(message.Questions);
        }

        [Fact]
        public void ReadQuery5Packet()
        {
            int offset = 0;
            var message = DnsMessageReader.ReadDnsResponse(SampleDnsPackets.Query5, ref offset);
            Assert.Equal(SampleDnsPackets.Query5.Length, offset);

            Assert.Equal(DnsQueryClass.IN, message.QueryClass);
            Assert.Equal(DnsQueryType.A, message.QueryType);
            Assert.Equal(new[] { "roaming", "officeapps", "live", "com" }, message.Labels);
            Assert.Equal(0, message.AnswerRecordCount);
            Assert.Equal(0, message.AdditionalRecordCount);
            Assert.Equal(1, message.QuestionCount);
            Assert.Equal(0, message.NameServerRecordCount);
            Assert.Empty(message.Questions);
        }
    }
}
