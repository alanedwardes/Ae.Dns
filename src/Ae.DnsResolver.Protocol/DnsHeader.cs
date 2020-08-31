using Ae.DnsResolver.Protocol.Enums;
using System;

namespace Ae.DnsResolver.Protocol
{
    public sealed class DnsHeader
    {
        public static ushort GenerateId() => ByteExtensions.ReadUInt16(Guid.NewGuid().ToByteArray());

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

        public ushort Id { get; set; }
        internal ushort Flags { get; set; }
        public short QuestionCount { get; set; }
        public short AnswerRecordCount { get; set; }
        public short NameServerRecordCount { get; set; }
        public short AdditionalRecordCount { get; set; }
        internal string[] Labels { get; set; }
        public DnsQueryType QueryType { get; set; }
        public DnsQueryClass QueryClass { get; set; }

        public bool IsQueryResponse
        {
            get => (Flags & 0x8000) == 0x8000;
            set => Flags = value ? (ushort)(Flags | 0x8000) : (ushort)(Flags & (~0x8000));
        }

        public DnsOperationCode OperationCode
        {
            get => (DnsOperationCode)((Flags & 0x7800) >> 11);
            set => Flags = (ushort)((Flags & ~0x7800) | ((int)value << 11));
        }

        public bool AuthoritativeAnswer
        {
            get => (Flags & 0x0400) == 0x0400;
            set => Flags = value ? (ushort)(Flags | 0x0400) : (ushort)(Flags & (~0x0400));
        }

        public bool Truncation
        {
            get => (Flags & 0x0200) == 0x0200;
            set => Flags = value ? (ushort)(Flags | 0x0200) : (ushort)(Flags & (~0x0200));
        }

        public bool RecusionDesired
        {
            get => (Flags & 0x0100) == 0x0100;
            set => Flags = value ? (ushort)(Flags | 0x0100) : (ushort)(Flags & (~0x0100));
        }

        public bool RecursionAvailable
        {
            get => (Flags & 0x0080) == 0x0080;
            set => Flags = value ? (ushort)(Flags | 0x0080) : (ushort)(Flags & (~0x0080));
        }

        public DnsResponseCode ResponseCode
        {
            get => (DnsResponseCode)(Flags & 0x000F);
            set => Flags = (ushort)((Flags & ~0x000F) | (byte)value);
        }

        public string Host
        {
            get => string.Join(".", Labels);
            set => Labels = value.Split(".");
        }

        public override string ToString() => $"Id: {Id}, Domain: {Host}, type: {QueryType}, class: {QueryClass}";
    }
}
