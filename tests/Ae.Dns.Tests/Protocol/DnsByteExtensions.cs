using Ae.Dns.Protocol;
using System;
using Xunit;

namespace Ae.Dns.Tests.Protocol
{
    public class DnsByteExtensionsTests
    {
        [Fact]
        public void TestSlice()
        {
            var source = new ArraySegment<byte>(new byte[] { 0, 1, 2, 3 });

            // The "else" block is testing framework, but it
            // ensures results across runtimes are the same
#if NETCOREAPP2_1
            var sliced = DnsByteExtensions.Slice(source, 1, 2);
#else
            var sliced = source.Slice(1, 2);
#endif

            Assert.Equal(new byte[] { 1, 2 }, sliced);
        }

        [Fact]
        public void TestSliceWithoutLength()
        {
            var source = new ArraySegment<byte>(new byte[] { 0, 1, 2, 3 });

            // The "else" block is testing framework, but it
            // ensures results across runtimes are the same
#if NETCOREAPP2_1
            var sliced = DnsByteExtensions.Slice(source, 2);
#else
            var sliced = source.Slice(2);
#endif

            Assert.Equal(new byte[] { 2, 3 }, sliced);
        }
        }
}
