using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol
{
    public sealed class DnsAnswer : IEquatable<DnsAnswer>, IDnsByteArrayReader
    {
        public DnsHeader Header { get; set; } = new DnsHeader();

        public IList<DnsResourceRecord> Answers { get; set; } = new List<DnsResourceRecord>();

        public bool Equals(DnsAnswer other) => Header.Equals(other.Header) && Answers.SequenceEqual(other.Answers);

        public override bool Equals(object obj) => obj is DnsAnswer record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(Header, Answers);

        public void ReadBytes(byte[] bytes, ref int offset)
        {
            Header.ReadBytes(bytes, ref offset);

            var records = new List<DnsResourceRecord>();
            for (var i = 0; i < Header.AnswerRecordCount + Header.NameServerRecordCount; i++)
            {
                records.Add(bytes.FromBytes<DnsResourceRecord>(ref offset));
            }
            Answers = records.ToArray();
        }

        public override string ToString() => $"RESPONSE: {Header}";

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Header.ToBytes();
            yield return Answers.Select(x => x.ToBytes()).SelectMany(x => x);
        }
    }
}
