using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Client
{
    public class DnsHeader
    {
        public ushort Id { get; set; }
        public ushort Header { get; set; }
        public short Qdcount { get; set; }
        public short Ancount { get; set; }
        public short Nscount { get; set; }
        public short Arcount { get; set; }
        public string[] Labels { get; set; }
        public DnsQueryType Qtype { get; set; }
        public DnsQueryClass Qclass { get; set; }

        public bool Qr
        {
            get
            {
                return false;
            }
            set
            {
                return;
            }
        }
    }

    public class DnsRequestMessage : DnsHeader
    {
        public override string ToString() => $"REQUEST: Id: {Id}, Domain: {string.Join(".", Labels)}, type: {Qtype}, class: {Qclass}";

        public byte[] ToBytes()
        {
            var parts = new List<byte>();

            parts.AddRange(Labels.WriteStrings());
            //parts.Add(type)

            return null;
        }
    }

    public class DnsResponseMessage : DnsHeader
    {
        public DnsResourceRecord[] Answers;
        public DnsQuestionRecord[] Questions;

        public override string ToString() => $"RESPONSE: Id: {Id}, Domain: {string.Join(".", Labels)}, type: {Qtype}, class: {Qclass}, records: {Answers.Length}";
    }

    public class DnsResourceRecord
    {
        public string[] Name;
        public DnsQueryType Type;
        public DnsQueryClass Class;
        public TimeSpan Ttl;
        public int DataOffset;
        public int DataLength;
    }

    public class DnsQuestionRecord
    {
        public string[] Name;
        public DnsQueryType Type;
        public DnsQueryClass Class;
    }

    public static class DnsMessageReader
    {
        public static DnsHeader ReadDnsMessage(byte[] bytes)
        {
            var result = new DnsRequestMessage();
            var offset = 0;
            ReadDnsMessage(bytes, result, ref offset);
            return result;
        }

        public static DnsResponseMessage ReadDnsResponse(byte[] bytes, ref int offset)
        {
            var result = new DnsResponseMessage();
            ReadDnsMessage(bytes, result, ref offset);

            var records = new List<DnsResourceRecord>();
            for (var i = 0; i < result.Ancount + result.Nscount; i++)
            {
                records.Add(ReadResourceRecord(bytes, ref offset));
            }
            result.Answers = records.ToArray();

            var questions = new List<DnsQuestionRecord>();
            for (var i = 0; i < result.Qdcount; i++)
            {
                //questions.Add(ReadQuestionRecord(bytes, ref offset));
            }
            result.Questions = questions.ToArray();

            return result;
        }

        private static DnsQuestionRecord ReadQuestionRecord(byte[] bytes, ref int offset)
        {
            var resourceName = bytes.ReadString(ref offset);
            var resourceType = (DnsQueryType)bytes.ReadUInt16(ref offset);
            var resourceClass = (DnsQueryClass)bytes.ReadUInt16(ref offset);

            return new DnsQuestionRecord
            {
                Name = resourceName.ToArray(),
                Class = resourceClass,
                Type = resourceType
            };
        }

        private static DnsResourceRecord ReadResourceRecord(byte[] bytes, ref int offset)
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
                Ttl = TimeSpan.FromSeconds(ttl),
                DataOffset = dataOffset,
                DataLength = rdlength
            };
        }

        private static DnsHeader ReadDnsMessage(byte[] bytes, DnsHeader result, ref int offset)
        {
            result.Id = bytes.ReadUInt16(ref offset);
            result.Header = bytes.ReadUInt16(ref offset);
            result.Qdcount = bytes.ReadInt16(ref offset);
            result.Ancount = bytes.ReadInt16(ref offset);
            result.Nscount = bytes.ReadInt16(ref offset);
            result.Arcount = bytes.ReadInt16(ref offset);
            result.Labels = bytes.ReadString(ref offset).ToArray();
            result.Qtype = (DnsQueryType)bytes.ReadInt16(ref offset);
            result.Qclass = (DnsQueryClass)bytes.ReadInt16(ref offset);
            return result;
        }
    }
}
