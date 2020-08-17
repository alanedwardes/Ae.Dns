using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Protocol
{
    public class DnsRequestMessage : DnsHeader
    {
        public override string ToString() => $"REQUEST: Id: {Id}, Domain: {string.Join(".", Labels)}, type: {QueryType}, class: {QueryClass}";
    }

    public class DnsResponseMessage : DnsHeader
    {
        public DnsResourceRecord[] Answers;
        public DnsQuestionRecord[] Questions;

        public override string ToString() => $"RESPONSE: Id: {Id}, Domain: {string.Join(".", Labels)}, type: {QueryType}, class: {QueryClass}, records: {Answers.Length}";
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
            for (var i = 0; i < result.AnswerRecordCount + result.NameServerRecordCount; i++)
            {
                records.Add(ReadResourceRecord(bytes, ref offset));
            }
            result.Answers = records.ToArray();

            var questions = new List<DnsQuestionRecord>();
            for (var i = 0; i < result.QuestionCount; i++)
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
            result.Flags = bytes.ReadUInt16(ref offset);
            result.QuestionCount = bytes.ReadInt16(ref offset);
            result.AnswerRecordCount = bytes.ReadInt16(ref offset);
            result.NameServerRecordCount = bytes.ReadInt16(ref offset);
            result.AdditionalRecordCount = bytes.ReadInt16(ref offset);
            result.Labels = bytes.ReadString(ref offset).ToArray();
            result.QueryType = (DnsQueryType)bytes.ReadInt16(ref offset);
            result.QueryClass = (DnsQueryClass)bytes.ReadInt16(ref offset);
            return result;
        }
    }
}
