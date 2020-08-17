using System;
using System.Collections.Generic;
using System.Text;

namespace Ae.DnsResolver.Protocol
{
    public static class ByteExtensions
    {
        public static short SwapEndian(this short val)
        {
            return BitConverter.IsLittleEndian ? (short)((val << 8) | (val >> 8)) : val;
        }

        public static ushort SwapEndian(this ushort val)
        {
            return BitConverter.IsLittleEndian ? (ushort)((val << 8) | (val >> 8)) : val;
        }

        public static uint SwapEndian(this uint val)
        {
            return BitConverter.IsLittleEndian ? (uint)((val << 16) | (val >> 16)) : val;
        }

        public static int SwapEndian(this int val)
        {
            return BitConverter.IsLittleEndian ? ((val << 16) | (val >> 16)) : val;
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

        public static DnsHeader ReadDnsHeader(this byte[] bytes, ref int offset)
        {
            var header = new DnsHeader();
            header.Id = bytes.ReadUInt16(ref offset);
            header.Flags = bytes.ReadUInt16(ref offset);
            header.QuestionCount = bytes.ReadInt16(ref offset);
            header.AnswerRecordCount = bytes.ReadInt16(ref offset);
            header.NameServerRecordCount = bytes.ReadInt16(ref offset);
            header.AdditionalRecordCount = bytes.ReadInt16(ref offset);
            header.Labels = bytes.ReadString(ref offset);
            header.QueryType = (DnsQueryType)bytes.ReadInt16(ref offset);
            header.QueryClass = (DnsQueryClass)bytes.ReadInt16(ref offset);
            return header;
        }

        public static IEnumerable<byte> WriteStrings(string[] strings)
        {
            foreach (var str in strings)
            {
                yield return (byte)str.Length;
                foreach (var c in str)
                {
                    yield return (byte)c;
                }
            }

            yield return 0;
        }

        public static IEnumerable<byte> WriteDnsHeader(this DnsHeader header)
        {
            var id = header.Id.SwapEndian();
            yield return (byte)id;
            yield return (byte)(id >> 8);

            var headerSwapped = header.Flags.SwapEndian();
            yield return (byte)headerSwapped;
            yield return (byte)(headerSwapped >> 8);

            var qdcount = header.QuestionCount.SwapEndian();
            yield return (byte)qdcount;
            yield return (byte)(qdcount >> 8);

            var ancount = header.AnswerRecordCount.SwapEndian();
            yield return (byte)ancount;
            yield return (byte)(ancount >> 8);

            var nscount = header.AnswerRecordCount.SwapEndian();
            yield return (byte)nscount;
            yield return (byte)(nscount >> 8);

            var arcount = header.AnswerRecordCount.SwapEndian();
            yield return (byte)arcount;
            yield return (byte)(arcount >> 8);

            foreach (var b in WriteStrings(header.Labels))
            {
                yield return b;
            }

            var type = ((ushort)header.QueryType).SwapEndian();
            yield return (byte)type;
            yield return (byte)(type >> 8);

            var qclass = ((ushort)header.QueryClass).SwapEndian();
            yield return (byte)qclass;
            yield return (byte)(qclass >> 8);
        }
    }
}
