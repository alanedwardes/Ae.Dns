using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string ToDebugString(this IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            return $"new [] {{{string.Join(", ", bytes)}}}";
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

            int? compressionOffset = null;
            while (true)
            {
                // get segment length or detect termination of segments
                int segmentLength = bytes[offset];

                // compressed name
                if (((DnsLabelType)segmentLength).HasFlag(DnsLabelType.Compressed))
                {
                    offset++;
                    if (!compressionOffset.HasValue)
                    {
                        // only record origin, and follow all pointers thereafter
                        compressionOffset = offset;
                    }

                    var mask = (1 << 14) - 1;
                    var pointer = ((ushort)(segmentLength + (bytes[offset] << 8))).SwapEndian() & mask;

                    if (segmentLength > (byte)DnsLabelType.Compressed)
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

                if (segmentLength == (byte)DnsLabelType.Normal)
                {
                    if (compressionOffset.HasValue)
                    {
                        offset = compressionOffset.Value;
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
            header.QueryType = (DnsQueryType)bytes.ReadUInt16(ref offset);
            header.QueryClass = (DnsQueryClass)bytes.ReadUInt16(ref offset);
            return header;
        }

        public static DnsAnswer ReadDnsAnswer(this byte[] bytes, ref int offset)
        {
            var result = new DnsAnswer();
            result.Header = bytes.ReadDnsHeader(ref offset);

            var records = new List<DnsResourceRecord>();
            for (var i = 0; i < result.Header.AnswerRecordCount + result.Header.NameServerRecordCount; i++)
            {
                records.Add(ReadDnsResourceRecord(bytes, ref offset));
            }
            result.Answers = records.ToArray();
            return result;
        }

        private static DnsResourceRecord ReadDnsResourceRecord(byte[] bytes, ref int offset)
        {
            var resourceName = bytes.ReadString(ref offset);
            var resourceType = (DnsQueryType)bytes.ReadUInt16(ref offset);
            var resourceClass = (DnsQueryClass)bytes.ReadUInt16(ref offset);
            var ttl = bytes.ReadUInt32(ref offset);
            var rdlength = bytes.ReadUInt16(ref offset);

            var dataOffset = offset;

            offset += rdlength;

            return new DnsResourceRecord
            {
                Name = resourceName.ToArray(),
                Type = resourceType,
                Class = resourceClass,
                Ttl = ttl,
                DataOffset = dataOffset,
                DataLength = rdlength
            };
        }

        public static IEnumerable<byte> ToBytes(this string[] strings)
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

        public static IEnumerable<byte> ToBytes(this ushort value)
        {
            var valueSwapped = value.SwapEndian();
            yield return (byte)valueSwapped;
            yield return (byte)(valueSwapped >> 8);
        }

        public static IEnumerable<byte> ToBytes(this short value)
        {
            var valueSwapped = value.SwapEndian();
            yield return (byte)valueSwapped;
            yield return (byte)(valueSwapped >> 8);
        }

        public static IEnumerable<byte> ToBytes(this uint value)
        {
            var valueSwapped = value.SwapEndian();
            yield return (byte)valueSwapped;
            yield return (byte)(valueSwapped >> 16);
        }

        public static IEnumerable<byte> WriteDnsHeader(this DnsHeader header)
        {
            IEnumerable<IEnumerable<byte>> Write()
            {
                yield return header.Id.ToBytes();
                yield return header.Flags.ToBytes();
                yield return header.QuestionCount.ToBytes();
                yield return header.AnswerRecordCount.ToBytes();
                yield return header.NameServerRecordCount.ToBytes();
                yield return header.AdditionalRecordCount.ToBytes();
                yield return header.Labels.ToBytes();
                yield return ((ushort)header.QueryType).ToBytes();
                yield return ((ushort)header.QueryClass).ToBytes();
            }

            return Write().SelectMany(x => x);
        }

        public static IEnumerable<byte> WriteDnsAnswer(this DnsAnswer answer)
        {
            IEnumerable<IEnumerable<byte>> Write(DnsResourceRecord resourceRecord)
            {
                yield return resourceRecord.Name.ToBytes();
                yield return ((ushort)resourceRecord.Type).ToBytes();
                yield return ((ushort)resourceRecord.Class).ToBytes();
                yield return resourceRecord.Ttl.ToBytes();
            }

            var header = answer.Header.WriteDnsHeader();
            var answers = answer.Answers.Select(Write).SelectMany(x => x).SelectMany(x => x);
            return header.Concat(answers);
        }
    }
}
