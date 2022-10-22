using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Provides extension methods around reading and writing <see cref="byte"/> buffers.
    /// </summary>
    public static class DnsByteExtensions
    {
        private static short ReadInt16(params byte[] bytes)
        {
            return (short)(bytes[0] << 0 |
                           bytes[1] << 8);
        }

        internal static ushort ReadUInt16(params byte[] bytes) => (ushort)ReadInt16(bytes);

        internal static short ReadInt16(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = ReadInt16(bytes[offset + 1], bytes[offset + 0]);
            offset += sizeof(short);
            return value;
        }

        /// <summary>
        /// Read a <see cref="ushort"/> from the specified byte array, incrementing offset by the length of the data read.
        /// </summary>
        /// <param name="bytes">The buffer to read from.</param>
        /// <param name="offset">The offset to increment.</param>
        /// <returns>The read unsigned short.</returns>
        public static ushort ReadUInt16(ReadOnlySpan<byte> bytes, ref int offset) => (ushort)ReadInt16(bytes, ref offset);

        internal static int ReadInt32(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = bytes[offset + 3] << 0 |
                        bytes[offset + 2] << 8 |
                        bytes[offset + 1] << 16 |
                        bytes[offset + 0] << 24;
            offset += sizeof(int);
            return value;
        }

        internal static uint ReadUInt32(ReadOnlySpan<byte> bytes, ref int offset) => (uint)ReadInt32(bytes, ref offset);

        /// <summary>
        /// Convert the specified byte array to a C# statement to initialise it in code.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>The new array statement for use in C# code.</returns>
        public static string ToDebugString(IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            return $"new byte [] {{{string.Join(",", bytes)}}}";
        }

        /// <summary>
        /// Read the specified number of bytes from the buffer, incrementing offset by the length of the data read.
        /// </summary>
        /// <param name="bytes">The buffer to read from.</param>
        /// <param name="length">The number of byes to read.</param>
        /// <param name="offset">The offset to increment.</param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> ReadBytes(ReadOnlySpan<byte> bytes, int length, ref int offset)
        {
            var data = bytes.Slice(offset, length);
            offset += length;
            return data;
        }

        internal static IList<string> ReadString(ReadOnlySpan<byte> bytes, ref int offset, int? maxOffset = int.MaxValue)
        {
            var parts = new List<string>();

            int? originalOffset = null;
            while (offset < bytes.Length && offset < maxOffset)
            {
                byte currentByte = bytes[offset];
                bool isCompressed = (currentByte & (1 << 6)) != 0 && (currentByte & (1 << 7)) != 0;
                bool isEnd = currentByte == 0;

                if (isCompressed)
                {
                    var compressedOffset = (ushort)ReadInt16(bytes[offset + 1], (byte)(currentByte & (1 << 6) - 1));

                    // Ensure the computed offset isn't outside the buffer
                    if (compressedOffset < maxOffset)
                    {
                        if (!originalOffset.HasValue)
                        {
                            originalOffset = ++offset;
                        }

                        offset = compressedOffset;
                        continue;
                    }
                }

                if (isEnd)
                {
                    if (originalOffset.HasValue)
                    {
                        offset = originalOffset.Value;
                    }

                    offset++;
                    break;
                }

                offset++;
                var str = Encoding.ASCII.GetString(bytes.Slice(offset, currentByte));
                parts.Add(str);
                offset += currentByte;
            }

            return parts;
        }

        /// <summary>
        /// Serialise the specified <see cref="IDnsByteArrayWriter"/> to a buffer.
        /// </summary>
        /// <param name="writer">The instance to serialise to bytes.</param>
        /// <returns>The serialised byte array.</returns>
        public static void ToBytes(IDnsByteArrayWriter writer, Span<byte> buffer, ref int offset)
        {
            writer.WriteBytes(buffer, ref offset);
        }

        public static ReadOnlySpan<byte> ToBytes(IDnsByteArrayWriter writer)
        {
            Span<byte> buffer = new byte[65527];
            var offset = 0;

            writer.WriteBytes(buffer, ref offset);

            return buffer.Slice(0, offset);
        }

        /// <summary>
        /// Deserialise the specified bytes to the supplied type.
        /// </summary>
        /// <typeparam name="TReader">The type to create from the byte array.</typeparam>
        /// <param name="bytes">The buffer to read from.</param>
        /// <returns>An instance of the type, created from the byte array.</returns>
        public static TReader FromBytes<TReader>(ReadOnlySpan<byte> bytes) where TReader : IDnsByteArrayReader, new()
        {
            var offset = 0;
            var result = FromBytes<TReader>(bytes, ref offset);
            if (offset != bytes.Length)
            {
                throw new InvalidOperationException($"Should have read {bytes.Length} bytes, only read {offset} bytes");
            }
            return result;
        }

        internal static TReader FromBytes<TReader>(ReadOnlySpan<byte> bytes, ref int offset) where TReader : IDnsByteArrayReader, new()
        {
            var reader = new TReader();
            reader.ReadBytes(bytes, ref offset);
            return reader;
        }

        /// <summary>
        /// Serialise the specified string array to a byte array.
        /// </summary>
        /// <param name="strings">The specified strings to serialise.</param>
        /// <returns>An enumerable of bytes representing the supplied strings.</returns>
        public static void ToBytes(IEnumerable<string> strings, Span<byte> buffer, ref int offset)
        {
            foreach (var str in strings)
            {
                buffer[offset++] = (byte)str.Length;
                foreach (var c in str)
                {
                    buffer[offset++] = (byte)c;
                }
            }

            buffer[offset++] = 0;
        }

        /// <summary>
        /// Serialise the specified <see cref="IConvertible"/> value to a byte array.
        /// </summary>
        /// <param name="value">The specified value to convert to a byte array.</param>
        /// <param name="buffer">The buffer to write into.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <returns>An enumerable of bytes representing the supplied <see cref="IConvertible"/> value.</returns>
        public static void ToBytes(IConvertible value, Span<byte> buffer, ref int offset)
        {
            var typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Int32:
                    var int32 = (int)value;
                    buffer[offset++] = (byte)(int32 >> 24);
                    buffer[offset++] = (byte)(int32 >> 16);
                    buffer[offset++] = (byte)(int32 >> 8);
                    buffer[offset++] = (byte)(int32 >> 0);
                    break;
                case TypeCode.UInt32:
                    var uint32 = (uint)value;
                    buffer[offset++] = (byte)(uint32 >> 24);
                    buffer[offset++] = (byte)(uint32 >> 16);
                    buffer[offset++] = (byte)(uint32 >> 8);
                    buffer[offset++] = (byte)(uint32 >> 0);
                    break;
                case TypeCode.Int16:
                    var uint16 = (short)value;
                    buffer[offset++] = (byte)(uint16 >> 8);
                    buffer[offset++] = (byte)(uint16 >> 0);
                    break;
                case TypeCode.UInt16:
                    var int16 = (ushort)value;
                    buffer[offset++] = (byte)(int16 >> 8);
                    buffer[offset++] = (byte)(int16 >> 0);
                    break;
                default:
                    throw new NotImplementedException($"Unable to process type {typeCode} ({value})");
            }
        }
    }
}
