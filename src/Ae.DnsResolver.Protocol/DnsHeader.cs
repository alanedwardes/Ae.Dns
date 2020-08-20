namespace Ae.DnsResolver.Protocol
{

    public class DnsHeader
    {
        public ushort Id { get; set; }
        public ushort Flags { get; set; }
        public short QuestionCount { get; set; }
        public short AnswerRecordCount { get; set; }
        public short NameServerRecordCount { get; set; }
        public short AdditionalRecordCount { get; set; }
        public string[] Labels { get; set; }
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

        public override string ToString() => $"Id: {Id}, Domain: {GetDomain()}, type: {QueryType}, class: {QueryClass}";

        public string GetDomain() => string.Join(".", Labels);
    }
}
