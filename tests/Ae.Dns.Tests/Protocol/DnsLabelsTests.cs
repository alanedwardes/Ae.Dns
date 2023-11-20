using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public sealed class DnsLabelsTests
    {
        [Fact]
        public void TestEmptyLabels()
        {
            var labels = new DnsLabels();
            Assert.Empty(labels);
            Assert.Empty(labels.ToString());
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
            Assert.Equal(0, labels.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.

            Assert.Equal(labels, DnsLabels.Empty);
        }

        [Fact]
        public void TestFromArray()
        {
            DnsLabels labels = new [] { "one", "two", "three" };

            Assert.Equal(new[] { "one", "two", "three" }, labels);
        }

        [Fact]
        public void TestFromString()
        {
            var labels = new DnsLabels("one.two.three");

            Assert.Equal(new[] { "one", "two", "three" }, labels);
        }

        [Fact]
        public void TestToString()
        {
            DnsLabels labels = new[] { "one", "two", "three" };

            Assert.Equal("one.two.three", labels.ToString());
        }
    }
}
