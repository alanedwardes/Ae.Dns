namespace Ae.DnsResolver.Protocol
{
    public static class BitExtensions
    {
        private static ushort MakeShortMask(byte start, byte end)
        {
            var mask1 = (ushort)~((1 << 16 - end) - 1);
            var mask2 = (ushort)((1 << 16 - start) - 1);
            return (ushort)(mask1 & mask2);
        }

        public static ushort GetBits(this ushort value, byte start, byte end)
        {
            ushort mask = MakeShortMask(start, end);
            ushort masked = (ushort)(value & mask);
            return (ushort)(masked << start);
        }

        public static ushort SetBits(this ushort value, byte start, byte end, int newValue)
        {
            ushort mask = MakeShortMask(start, end);
            var maskedNewValue = (ushort)(newValue >> start) & mask;
            var maskedValue = (ushort)(value & ~mask);
            return (ushort)(maskedNewValue ^ maskedValue);
        }
    }
}
