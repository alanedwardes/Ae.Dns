using System;
using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsSoaRecord : DnsResourceRecord
    {
        public string MName { get; set; }
        public string RName { get; set; }
        public uint Serial { get; set; }
        public TimeSpan Refresh { get; set; }
        public TimeSpan Retry { get; set; }
        public TimeSpan Expire { get; set; }
        public TimeSpan Minimum { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset)
        {
            MName = string.Join('.', bytes.ReadString(ref offset));
            RName = string.Join('.', bytes.ReadString(ref offset));
            Serial = bytes.ReadUInt32(ref offset);
            Refresh = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Retry = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Expire = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Minimum = TimeSpan.FromSeconds(bytes.ReadUInt32(ref offset));
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return MName.Split('.').ToBytes();
            yield return RName.Split('.').ToBytes();
            yield return Serial.ToBytes();
            yield return ((int)Refresh.TotalSeconds).ToBytes();
            yield return ((int)Retry.TotalSeconds).ToBytes();
            yield return ((int)Expire.TotalSeconds).ToBytes();
            yield return ((uint)Minimum.TotalSeconds).ToBytes();
        }
    }
}
