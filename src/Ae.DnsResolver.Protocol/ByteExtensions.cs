using Ae.DnsResolver.Protocol.Enums;
using Ae.DnsResolver.Protocol.Records;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ae.DnsResolver.Protocol
{
    public static class ByteExtensions
    {
        public static short ReadInt16(params byte[] bytes)
        {
            return (short)(bytes[0] << 0 |
                           bytes[1] << 8);
        }

        public static ushort ReadUInt16(params byte[] bytes) => (ushort)ReadInt16(bytes);

        public static short ReadInt16(this byte[] bytes, ref int offset)
        {
            var value = ReadInt16(bytes[offset + 1], bytes[offset + 0]);
            offset += sizeof(short);
            return value;
        }

        public static ushort ReadUInt16(this byte[] bytes, ref int offset) => (ushort)bytes.ReadInt16(ref offset);

        public static int ReadInt32(this byte[] bytes, ref int offset)
        {
            var value = bytes[offset + 3] << 0 |
                        bytes[offset + 2] << 8 |
                        bytes[offset + 1] << 16 |
                        bytes[offset + 0] << 24;
            offset += sizeof(int);
            return value;
        }

        public static uint ReadUInt32(this byte[] bytes, ref int offset) => (uint)bytes.ReadInt32(ref offset);

        public static string ToDebugString(this IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            return $"new [] {{{string.Join(", ", bytes)}}}";
        }

        public static byte[] ReadBytes(this byte[] bytes, int length)
        {
            var offset = 0;
            return ReadBytes(bytes, length, ref offset);
        }

        public static byte[] ReadBytes(this byte[] bytes, int length, ref int offset)
        {
            var data = new byte[length];
            Array.Copy(bytes, offset, data, 0, length);
            offset += length;
            return data;
        }

        public static string[] ReadString(this byte[] bytes, ref int offset, int? maxOffset = int.MaxValue)
        {
            var parts = new List<string>();

            int? originalOffset = null;
            while (offset < bytes.Length && offset < maxOffset)
            {
                byte currentByte = bytes[offset];

                var bits = new BitArray(new[] { currentByte });

                bool isCompressed = bits[7] && bits[6] && !bits[5] && !bits[4];
                bool isEnd = currentByte == 0;

                if (isCompressed)
                {
                    offset++;
                    if (!originalOffset.HasValue)
                    {
                        originalOffset = offset;
                    }

                    offset = (ushort)ReadInt16(bytes[offset], (byte)(currentByte & (1 << 6) - 1));
                }
                else if (isEnd)
                {
                    if (originalOffset.HasValue)
                    {
                        offset = originalOffset.Value;
                    }
                    offset++;
                    break;
                }
                else
                {
                    offset++;
                    var str = Encoding.ASCII.GetString(bytes, offset, currentByte);
                    parts.Add(str);
                    offset += currentByte;
                }
            }

            return parts.ToArray();
        }

        public static DnsHeader ReadDnsHeader(this byte[] bytes)
        {
            var offset = 0;
            return ReadDnsHeader(bytes, ref offset);
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

        public static DnsAnswer ReadDnsAnswer(this byte[] bytes)
        {
            var offset = 0;
            return ReadDnsAnswer(bytes, ref offset);
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
            var recordName = bytes.ReadString(ref offset);
            var recordType = (DnsQueryType)bytes.ReadUInt16(ref offset);

            var record = DnsResourceRecord.CreateResourceRecord(recordType);
            record.Name = recordName;
            record.Type = recordType;
            record.Class = (DnsQueryClass)bytes.ReadUInt16(ref offset);
            record.Ttl = bytes.ReadUInt32(ref offset);
            record.DataLength = bytes.ReadUInt16(ref offset);
            record.ReadData(bytes, ref offset);

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

        public static IEnumerable<byte> ToBytes(this object value)
        {
            var typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Int32:
                    var int32 = (int)value;
                    yield return (byte)(int32 >> 24);
                    yield return (byte)(int32 >> 16);
                    yield return (byte)(int32 >> 8);
                    yield return (byte)(int32 >> 0);
                    break;
                case TypeCode.UInt32:
                    var uint32 = (uint)value;
                    yield return (byte)(uint32 >> 24);
                    yield return (byte)(uint32 >> 16);
                    yield return (byte)(uint32 >> 8);
                    yield return (byte)(uint32 >> 0);
                    break;
                case TypeCode.Int16:
                    var uint16 = (short)value;
                    yield return (byte)(uint16 >> 8);
                    yield return (byte)(uint16 >> 0);
                    break;
                case TypeCode.UInt16:
                    var int16 = (ushort)value;
                    yield return (byte)(int16 >> 8);
                    yield return (byte)(int16 >> 0);
                    break;
                default:
                    throw new NotImplementedException($"Unable to process type {typeCode} ({value})");
            }
        }

        public static IEnumerable<byte> ToBytes(this DnsHeader header)
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
                yield return header.QueryType.ToBytes();
                yield return header.QueryClass.ToBytes();
            }

            return Write().SelectMany(x => x);
        }

        public static IEnumerable<byte> ToBytes(this DnsAnswer answer)
        {
            IEnumerable<IEnumerable<byte>> Write(DnsResourceRecord resourceRecord)
            {
                yield return resourceRecord.Name.ToBytes();
                yield return resourceRecord.Type.ToBytes();
                yield return resourceRecord.Class.ToBytes();
                yield return resourceRecord.Ttl.ToBytes();
                var data = resourceRecord.WriteData().ToArray();
                yield return ((ushort)data.Length).ToBytes();
                yield return data;
            }

            var header = answer.Header.ToBytes();
            var answers = answer.Answers.Select(Write).SelectMany(x => x).SelectMany(x => x);
            return header.Concat(answers);
        }
    }
}
