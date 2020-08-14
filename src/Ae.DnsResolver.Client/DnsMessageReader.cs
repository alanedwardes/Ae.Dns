using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Client
{
    public abstract class DnsMessage
    {
        public short Id;
        public short Header;
        public short Qdcount;
        public short Ancount;
        public short Nscount;
        public short Arcount;

        public string[] Labels;
        public DnsQueryType Qtype;
        public DnsQueryClass Qclass;
    }

    public class DnsRequestMessage : DnsMessage
    {
        public override string ToString() => string.Format("REQUEST: Domain: {0}, type: {1}, class: {2}", string.Join(".", Labels), Qtype, Qclass);

        public byte[] ToBytes()
        {
            var parts = new List<byte>();

            parts.AddRange(Labels.WriteStrings());
            //parts.Add(type)

            return null;
        }
    }

    public class DnsResponseMessage : DnsMessage
    {
        public DnsResourceRecord[] Answers;
        public DnsQuestionRecord[] Questions;

        public override string ToString() => string.Format("RESPONSE: Domain: {0}, type: {1}, class: {2}, records: {3}", string.Join(".", Labels), Qtype, Qclass, Answers.Length);
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
        public static DnsMessage ReadDnsMessage(byte[] bytes)
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

        private static DnsMessage ReadDnsMessage(byte[] bytes, DnsMessage result, ref int offset)
        {
            result.Id = bytes.ReadInt16(ref offset);
            result.Header = bytes.ReadInt16(ref offset);
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
