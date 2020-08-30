using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsMxRecord : DnsResourceRecord
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset)
        {
            Preference = bytes.ReadUInt16(ref offset);
            Exchange = string.Join('.', bytes.ReadString(ref offset));
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Preference.ToBytes();
            yield return Exchange.Split('.').ToBytes();
        }
    }
}
