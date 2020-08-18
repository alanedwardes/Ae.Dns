using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.DnsResolver.Protocol
{
    public class DnsRequestMessage
    {
        public DnsHeader Header;

        public override string ToString() => $"REQUEST: {Header}";
    }

    public class DnsResponseMessage
    {
        public DnsHeader Header;

        public DnsResourceRecord[] Answers;
        public DnsQuestionRecord[] Questions;

        public override string ToString() => $"RESPONSE: {Header}";
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
        public static DnsResponseMessage ReadDnsResponse(byte[] bytes, ref int offset)
        {
            var result = new DnsResponseMessage();
            result.Header = bytes.ReadDnsHeader(ref offset);

            var records = new List<DnsResourceRecord>();
            for (var i = 0; i < result.Header.AnswerRecordCount + result.Header.NameServerRecordCount; i++)
            {
                records.Add(ReadResourceRecord(bytes, ref offset));
            }
            result.Answers = records.ToArray();

            var questions = new List<DnsQuestionRecord>();
            for (var i = 0; i < result.Header.QuestionCount; i++)
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
    }
}
