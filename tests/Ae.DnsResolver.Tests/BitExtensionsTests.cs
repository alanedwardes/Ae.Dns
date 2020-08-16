using Ae.DnsResolver.Protocol;
using System;
using Xunit;

namespace Ae.DnsResolver.Tests
{
    public class BitExtensionsTests
    {
        [Fact]
        public void Test()
        {
            var result1 = Convert.ToUInt16("0000000000110001", 2).GetBits(10, 12);
            Assert.Equal(Convert.ToUInt16("1100000000000000", 2), result1);

            var result2 = ushort.MinValue.SetBits(10, 12, Convert.ToUInt16("1100100000000000", 2));
            Assert.Equal(Convert.ToUInt16("0000000000110000", 2), result2);

            var result3 = ushort.MinValue.SetBits(10, 12, Convert.ToUInt16("1000000000000000", 2));
            Assert.Equal(Convert.ToUInt16("0000000000100000", 2), result3);

            var result4 = ushort.MinValue.SetBits(10, 12, Convert.ToUInt16("0100000000000000", 2));
            Assert.Equal(Convert.ToUInt16("0000000000010000", 2), result4);
        }

        [Theory]
        [InlineData("1000000000110001", 0, 12,  "1000000000110000")]
        [InlineData("1111111111111111", 0, 12,  "1111111111110000")]
        [InlineData("0000000000110001", 10, 12, "1100000000000000")]
        [InlineData("0000000000110001", 15, 16, "1000000000000000")]
        public void TestGetBitsShort(string input, byte start, byte end, string expected)
        {
            var result = Convert.ToUInt16(input, 2).GetBits(start, end);
            Assert.Equal(expected, Convert.ToString(result, 2).PadLeft(16, '0'));
        }

        [Theory]
        [InlineData("0000000000000000", 0, 2,   "1100000000000000", "1100000000000000")]
        [InlineData("0000000000000000", 1, 2,   "1000000000000000", "0100000000000000")]
        [InlineData("0000000001111000", 10, 12, "1000000000000000", "0000000001101000")]
        [InlineData("0000000000000000", 10, 12, "1110000000000000", "0000000000110000")]
        [InlineData("1111111111111111", 2, 12,  "0100000000000000", "1101000000001111")]
        [InlineData("0000000000000000", 4, 12,  "1111111100000000", "0000111111110000")]
        public void TestSetBitsShort(string input, byte start, byte end, string replacement, string expected)
        {
            var result = Convert.ToUInt16(input, 2).SetBits(start, end, Convert.ToUInt16(replacement, 2));
            Assert.Equal(expected, Convert.ToString(result, 2).PadLeft(16, '0'));
        }
    }
}
