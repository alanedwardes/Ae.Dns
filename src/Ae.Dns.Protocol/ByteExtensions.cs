using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ae.Dns.Protocol
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

        public static byte[] ToBytes(this IDnsByteArrayWriter writer)
        {
            return writer.WriteBytes().SelectMany(x => x).ToArray();
        }

        public static TReader FromBytes<TReader>(this byte[] bytes) where TReader : IDnsByteArrayReader, new()
        {
            var offset = 0;
            return bytes.FromBytes<TReader>(ref offset);
        }

        public static TReader FromBytes<TReader>(this byte[] bytes, ref int offset) where TReader : IDnsByteArrayReader, new()
        {
            var reader = new TReader();
            reader.ReadBytes(bytes, ref offset);
            return reader;
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
    }
}
