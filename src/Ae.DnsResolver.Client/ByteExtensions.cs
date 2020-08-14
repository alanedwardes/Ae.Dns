using System;
using System.Collections.Generic;
using System.Text;

namespace Ae.DnsResolver.Client
{
    public static class ByteExtensions
    {
        private static short SwapEndian(this short val)
        {
            return BitConverter.IsLittleEndian ? (short)((val << 8) | (val >> 8)) : val;
        }

        private static ushort SwapEndian(this ushort val)
        {
            return BitConverter.IsLittleEndian ? (ushort)((val << 8) | (val >> 8)) : val;
        }

        private static uint SwapEndian(this uint val)
        {
            return BitConverter.IsLittleEndian ? (uint)((val << 16) | (val >> 16)) : val;
        }

        public static byte ReadByte(this byte[] bytes, ref int offset)
        {
            return bytes[offset++];
        }

        public static short ReadInt16(this byte[] bytes, ref int offset)
        {
            offset += sizeof(short);
            return BitConverter.ToInt16(bytes, offset - sizeof(short)).SwapEndian();
        }

        public static ushort ReadUInt16(this byte[] bytes, ref int offset)
        {
            offset += sizeof(ushort);
            return BitConverter.ToUInt16(bytes, offset - sizeof(ushort)).SwapEndian();
        }

        public static byte[] WriteUInt16(this ushort value)
        {
            return BitConverter.GetBytes(value.SwapEndian());
        }

        public static uint ReadUInt32(this byte[] bytes, ref int offset)
        {
            offset += sizeof(uint);
            return BitConverter.ToUInt32(bytes, offset - sizeof(uint)).SwapEndian();
        }

        public static byte[] ReadBytes(this byte[] bytes, int length, ref int offset)
        {
            var data = new byte[length];
            Array.Copy(bytes, offset, data, 0, length);
            offset += length;
            return data;
        }

        public static string[] ReadString(this byte[] bytes, ref int offset)
        {
            var parts = new List<string>();

            int compressionOffset = -1;
            while (true)
            {
                // get segment length or detect termination of segments
                int segmentLength = bytes[offset];

                // compressed name
                if ((segmentLength & 0xC0) == 0xC0)
                {
                    offset++;
                    if (compressionOffset == -1)
                    {
                        // only record origin, and follow all pointers thereafter
                        compressionOffset = offset;
                    }

                    var mask = (1 << 14) - 1;
                    var pointer = ((ushort)(segmentLength + (bytes[offset] << 8))).SwapEndian() & mask;

                    if (segmentLength != 192)
                    {
                        offset = pointer;
                        segmentLength = bytes[offset];
                    }
                    else
                    {
                        // move pointer to compression segment
                        offset = bytes[offset];
                        segmentLength = bytes[offset];
                    }
                }

                if (segmentLength == 0x00)
                {
                    if (compressionOffset != -1)
                    {
                        offset = compressionOffset;
                    }
                    // move past end of name \0
                    offset++;
                    break;
                }

                // move pass length and get segment text
                offset++;
                parts.Add(Encoding.ASCII.GetString(bytes, offset, segmentLength));
                offset += segmentLength;
            }

            return parts.ToArray();
        }

        public static IEnumerable<byte> WriteStrings(this string[] strings)
        {
            foreach (var str in strings)
            {
                yield return (byte)str.Length;
                foreach (var character in str)
                {
                    yield return (byte)character;
                }
            }
            yield return 0;
        }
    }
}
