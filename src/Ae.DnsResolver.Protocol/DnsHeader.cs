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

        public bool IsQuery
        {
            get => (Flags.GetBits(0, 1) >> 15) == 1;
            set => Flags.SetBits(0, 1, (ushort)(value ? 1 : 0));
        }

        public DnsOperationCode OperationCode
        {
            get => (DnsOperationCode)(Flags.GetBits(1, 5) >> 8).SwapEndian();
            set => Flags.SetBits(1, 5, (ushort)value);
        }

        public bool AuthoritativeAnswer
        {
            get => (Flags.GetBits(5, 6) >> 15) == 1;
            set => Flags.SetBits(5, 6, (ushort)(value ? 1 : 0));
        }

        public bool Truncation
        {
            get => (Flags.GetBits(6, 7) >> 15) == 1;
            set => Flags.SetBits(6, 7, (ushort)(value ? 1 : 0));
        }

        public bool RecusionDesired
        {
            get => (Flags.GetBits(7, 8) >> 15) == 1;
            set => Flags.SetBits(7, 8, (ushort)(value ? 1 : 0));
        }

        public bool RecursionAvailable
        {
            get => (Flags.GetBits(8, 9) >> 15) == 1;
            set => Flags.SetBits(8, 9, (ushort)(value ? 1 : 0));
        }

        public byte Reserved
        {
            get => (byte)(Flags.GetBits(9, 12) >> 8);
            set => Flags.SetBits(9, 12, value);
        }

        public DnsResponseCode ResponseCode
        {
            get => (DnsResponseCode)(Flags.GetBits(12, 16) >> 8).SwapEndian();
            set => Flags.SetBits(12, 16, ((ushort)value).SwapEndian());
        }

        public override string ToString() => $"Id: {Id}, Domain: {string.Join(".", Labels)}, type: {QueryType}, class: {QueryClass}";
    }
}
