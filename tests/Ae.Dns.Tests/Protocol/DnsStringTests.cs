using System;
using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsStringTests
    {
        [Theory]
        [InlineData(2, 48, new[] { "alanedwardes-my", "sharepoint", "com" })]
        [InlineData(2, 105, new[] { "alanedwardes", "sharepoint", "com" })]
        [InlineData(2, 153, new[] { "302-ipv4e", "clump", "dprodmgd104", "aa-rt", "sharepoint", "com" })]
        [InlineData(2, 185, new[] { "187170-ipv4e", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com" })]
        [InlineData(2, 260, new[] { "187170-ipv4e", "farm", "dprodmgd104", "sharepointonline", "com", "akadns", "net" })]
        [InlineData(2, 344, new[] { "187170-ipv4", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com", "spo-0004", "spo-msedge", "net" })]
        public void ReadStringTests(int example, int offset, string[] expected)
        {
            var value = DnsByteExtensions.ReadLabels(SampleDnsPackets.Answers[example - 1], ref offset);
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(2, 90, new[] { "alanedwardes", "sharepoint", "com" })]
        [InlineData(2, 117, new[] { "302-ipv4e", "clump", "dprodmgd104", "aa-rt", "sharepoint", "com" })]
        [InlineData(2, 165, new[] { "187170-ipv4e", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com" })]
        [InlineData(2, 197, new[] { "187170-ipv4e", "farm", "dprodmgd104", "sharepointonline", "com", "akadns", "net" })]
        [InlineData(2, 272, new[] { "187170-ipv4", "farm", "dprodmgd104", "aa-rt", "sharepoint", "com", "spo-0004", "spo-msedge", "net" })]
        [InlineData(4, 104, new[] { "alanedwardes", "com" })]
        public void ReadCnameRecordTests(int example, int offset, string[] expected)
        {
            var value = DnsByteExtensions.ReadLabels(SampleDnsPackets.Answers[example - 1], ref offset);
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(3, 50, new byte[] { 216, 58, 210, 206 })]
        [InlineData(4, 118, new byte[] { 143, 204, 191, 46 })]
        [InlineData(4, 134, new byte[] { 143, 204, 191, 37 })]
        [InlineData(4, 150, new byte[] { 143, 204, 191, 71 })]
        [InlineData(4, 166, new byte[] { 143, 204, 191, 110 })]
        public void ReadARecordTests(int example, int offset, byte[] expected)
        {
            var value = DnsByteExtensions.ReadBytes(SampleDnsPackets.Answers[example - 1], 4, ref offset);
            Assert.Equal(expected, value.ToArray());
        }

        [Fact]
        public void ReadInfiniteLoop()
        {
            var offset = 0;
            Assert.Throws<InvalidOperationException>(() => DnsByteExtensions.ReadString(new byte[] { 0xC0, 0x00 }, ref offset, true));
        }
    }
}
