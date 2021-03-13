using Ae.Dns.Protocol.Enums;
using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a DNS header, used to represent a DNS query and a DNS query result.
    /// </summary>
    public sealed class DnsHeader : IEquatable<DnsHeader>, IDnsByteArrayReader, IDnsByteArrayWriter
    {
        /// <summary>
        /// Generate a unique ID to identify this DNS message.
        /// </summary>
        /// <returns>A random <see cref="ushort"/> value.</returns>
        public static ushort GenerateId() => ByteExtensions.ReadUInt16(Guid.NewGuid().ToByteArray());

        /// <summary>
        /// Create a DNS query using the specified host name and DNS query type.
        /// </summary>
        /// <param name="host">The DNS host to request in the query.</param>
        /// <param name="type">The type of DNS query to request.</param>
        /// <returns>The complete DNS query.</returns>
        public static DnsHeader CreateQuery(string host, DnsQueryType type = DnsQueryType.A)
        {
            return new DnsHeader
            {
                Id = GenerateId(),
                Host = host,
                QueryType = type,
                QueryClass = DnsQueryClass.IN,
                OperationCode = DnsOperationCode.QUERY,
                QuestionCount = 1,
                RecusionDesired = true
            };
        }

        /// <summary>
        /// The unique ID of this DNS query.
        /// </summary>
        public ushort Id { get; set; }

        internal ushort Flags { get; set; }

        /// <summary>
        /// The number of questions in this header.
        /// </summary>
        public short QuestionCount { get; set; }

        /// <summary>
        /// The number of <see cref="DnsAnswer"/> records in this header.
        /// </summary>
        public short AnswerRecordCount { get; set; }

        /// <summary>
        /// The number of name server records in this header.
        /// </summary>
        public short NameServerRecordCount { get; set; }

        /// <summary>
        /// The number of additional records in this header.
        /// </summary>
        public short AdditionalRecordCount { get; set; }

        internal string[] Labels { get; set; }

        /// <summary>
        /// The <see cref="DnsQueryType"/> of this header.
        /// </summary>
        public DnsQueryType QueryType { get; set; }

        /// <summary>
        /// The <see cref="DnsQueryClass"/> of this header, normally <see cref="DnsQueryClass.IN"/> (Internet).
        /// </summary>
        public DnsQueryClass QueryClass { get; set; }

        /// <summary>
        /// Determines whether this is a DNS query or answer.
        /// </summary>
        public bool IsQueryResponse
        {
            get => (Flags & 0x8000) == 0x8000;
            set => Flags = value ? (ushort)(Flags | 0x8000) : (ushort)(Flags & (~0x8000));
        }

        /// <summary>
        /// Determines the operation code for this DNS header.
        /// </summary>
        public DnsOperationCode OperationCode
        {
            get => (DnsOperationCode)((Flags & 0x7800) >> 11);
            set => Flags = (ushort)((Flags & ~0x7800) | ((int)value << 11));
        }

        /// <summary>
        /// Determines whether the answer was from an authoritative DNS server.
        /// </summary>
        public bool AuthoritativeAnswer
        {
            get => (Flags & 0x0400) == 0x0400;
            set => Flags = value ? (ushort)(Flags | 0x0400) : (ushort)(Flags & (~0x0400));
        }

        /// <summary>
        /// Determines whether this DNS answer was truncated due to size.
        /// </summary>
        public bool Truncation
        {
            get => (Flags & 0x0200) == 0x0200;
            set => Flags = value ? (ushort)(Flags | 0x0200) : (ushort)(Flags & (~0x0200));
        }

        /// <summary>
        /// Indicates whether recrusion is desired on this DNS query.
        /// If this is an answer, indicates whether the original query requested recursion.
        /// </summary>
        public bool RecusionDesired
        {
            get => (Flags & 0x0100) == 0x0100;
            set => Flags = value ? (ushort)(Flags | 0x0100) : (ushort)(Flags & (~0x0100));
        }

        /// <summary>
        /// Indicates whether recusion is available.
        /// </summary>
        public bool RecursionAvailable
        {
            get => (Flags & 0x0080) == 0x0080;
            set => Flags = value ? (ushort)(Flags | 0x0080) : (ushort)(Flags & (~0x0080));
        }

        /// <summary>
        /// The <see cref="DnsResponseCode"/> for this result.
        /// </summary>
        public DnsResponseCode ResponseCode
        {
            get => (DnsResponseCode)(Flags & 0x000F);
            set => Flags = (ushort)((Flags & ~0x000F) | (byte)value);
        }

        /// <summary>
        /// The DNS host to use, for example "example.com".
        /// </summary>
        public string Host
        {
            get => string.Join(".", Labels);
            set => Labels = value.Split('.');
        }


        /// <inheritdoc/>
        public override string ToString() => $"Id: {Id}, Domain: {Host}, type: {QueryType}, class: {QueryClass}";

        /// <inheritdoc/>
        public bool Equals(DnsHeader other)
        {
            return Id == other.Id &&
                   Flags == other.Flags &&
                   QuestionCount == other.QuestionCount &&
                   AnswerRecordCount == other.AnswerRecordCount &&
                   NameServerRecordCount == other.NameServerRecordCount &&
                   AdditionalRecordCount == other.AdditionalRecordCount &&
                   Host == other.Host &&
                   QueryType == other.QueryType &&
                   QueryClass == other.QueryClass;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Id);
            hash.Add(Flags);
            hash.Add(QuestionCount);
            hash.Add(AnswerRecordCount);
            hash.Add(NameServerRecordCount);
            hash.Add(AdditionalRecordCount);
            hash.Add(QueryType);
            hash.Add(QueryClass);
            hash.Add(Host);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset)
        {
            Id = bytes.ReadUInt16(ref offset);
            Flags = bytes.ReadUInt16(ref offset);
            QuestionCount = bytes.ReadInt16(ref offset);
            AnswerRecordCount = bytes.ReadInt16(ref offset);
            NameServerRecordCount = bytes.ReadInt16(ref offset);
            AdditionalRecordCount = bytes.ReadInt16(ref offset);
            Labels = bytes.ReadString(ref offset);
            QueryType = (DnsQueryType)bytes.ReadUInt16(ref offset);
            QueryClass = (DnsQueryClass)bytes.ReadUInt16(ref offset);
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Id.ToBytes();
            yield return Flags.ToBytes();
            yield return QuestionCount.ToBytes();
            yield return AnswerRecordCount.ToBytes();
            yield return NameServerRecordCount.ToBytes();
            yield return AdditionalRecordCount.ToBytes();
            yield return Labels.ToBytes();
            yield return QueryType.ToBytes();
            yield return QueryClass.ToBytes();
        }
    }
}
