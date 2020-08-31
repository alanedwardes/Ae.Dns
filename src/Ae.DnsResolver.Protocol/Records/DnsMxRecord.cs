using System;
using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsMxRecord : DnsResourceRecord, IEquatable<DnsMxRecord>
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }

        public bool Equals(DnsMxRecord other)
        {
            return Preference == other.Preference &&
                   Exchange == other.Exchange;
        }

        public override bool Equals(object obj) => obj is DnsMxRecord record ? Equals(record) : base.Equals(obj);

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
