using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Ae.DnsResolver.Protocol
{
    public static class ByteExtensions
    {
        public static short ReadInt16(this byte[] bytes, ref int offset)
        {
            var value = bytes[offset + 1] << 0 |
                        bytes[offset + 0] << 8;
            offset += sizeof(short);
            return (short)value;
        }

        public static ushort ReadUInt16(this byte[] bytes, ref int offset)
        {
            return (ushort)bytes.ReadInt16(ref offset);
        }

        public static int ReadInt32(this byte[] bytes, ref int offset)
        {
            var value = bytes[offset + 3] << 0 |
                        bytes[offset + 2] << 8 |
                        bytes[offset + 1] << 16 |
                        bytes[offset + 0] << 24;
            offset += sizeof(int);
            return value;
        }

        public static uint ReadUInt32(this byte[] bytes, ref int offset)
        {
            return (uint)bytes.ReadInt32(ref offset);
        }

        public static string ToDebugString(this IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            return $"new [] {{{string.Join(", ", bytes)}}}";
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
            while (offset < bytes.Length)
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
                    var pointerBytes = new byte[] { (byte)segmentLength, bytes[offset] };
                    var pointerOffset = 0;
                    var pointer = pointerBytes.ReadUInt16(ref pointerOffset) & mask;

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
            var record = new DnsResourceRecord();
            record.Name = bytes.ReadString(ref offset);
            record.Type = (DnsQueryType)bytes.ReadUInt16(ref offset);
            record.Class = (DnsQueryClass)bytes.ReadUInt16(ref offset);
            record.Ttl = bytes.ReadUInt32(ref offset);
            record.DataLength = bytes.ReadUInt16(ref offset);
            record.DataOffset = offset;

            var dataOffset = offset;

            if (record.Type == DnsQueryType.A || record.Type == DnsQueryType.AAAA)
            {
                record.IPAddress = new IPAddress(bytes.ReadBytes(record.DataLength, ref dataOffset));
            }
            else if (record.Type == DnsQueryType.CNAME || record.Type == DnsQueryType.TEXT)
            {
                record.Text = string.Join(".", bytes.ReadString(ref dataOffset));
            }
            else if (record.Type == DnsQueryType.MX)
            {
                record.MxRecord = new DnsMxRecord
                {
                    Preference = bytes.ReadUInt16(ref dataOffset),
                    Exchange = string.Join(".", bytes.ReadString(ref dataOffset))
                };
            }
            else if (record.Type == DnsQueryType.SOA)
            {
                record.SoaRecord = new DnsSoaRecord
                {
                    MName = string.Join(".", bytes.ReadString(ref dataOffset)),
                    RName = string.Join(".", bytes.ReadString(ref dataOffset)),
                    Serial = bytes.ReadUInt32(ref dataOffset),
                    Refresh = bytes.ReadInt32(ref dataOffset),
                    Retry = bytes.ReadInt32(ref dataOffset),
                    Expire = bytes.ReadInt32(ref dataOffset),
                    Minimum = bytes.ReadUInt32(ref dataOffset)
                };
            }

            offset += record.DataLength;

            return record;
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
            yield return (byte)(value >> 8);
            yield return (byte)(value >> 0);
        }

        public static IEnumerable<byte> ToBytes(this short value)
        {
            yield return (byte)(value >> 8);
            yield return (byte)(value >> 0);
        }

        public static IEnumerable<byte> ToBytes(this uint value)
        {
            yield return (byte)(value >> 24);
            yield return (byte)(value >> 16);
            yield return (byte)(value >> 8);
            yield return (byte)(value >> 0);
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
