using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsSoaRecord : DnsResourceRecord
    {
        public string MName { get; set; }
        public string RName { get; set; }
        public uint Serial { get; set; }
        public int Refresh { get; set; }
        public int Retry { get; set; }
        public int Expire { get; set; }
        public uint Minimum { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset)
        {
            MName = string.Join('.', bytes.ReadString(ref offset));
            RName = string.Join('.', bytes.ReadString(ref offset));
            Serial = bytes.ReadUInt32(ref offset);
            Refresh = bytes.ReadInt32(ref offset);
            Retry = bytes.ReadInt32(ref offset);
            Expire = bytes.ReadInt32(ref offset);
            Minimum = bytes.ReadUInt32(ref offset);
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return MName.Split('.').ToBytes();
            yield return RName.Split('.').ToBytes();
            yield return Serial.ToBytes();
            yield return Refresh.ToBytes();
            yield return Retry.ToBytes();
            yield return Expire.ToBytes();
            yield return Minimum.ToBytes();
        }
    }
}
