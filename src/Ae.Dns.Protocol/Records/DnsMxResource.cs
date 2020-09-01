using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Resources
{
    public sealed class DnsMxResource : IDnsResource, IEquatable<DnsMxResource>
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }

        public bool Equals(DnsMxResource other) => Preference == other.Preference && Exchange == other.Exchange;

        public override bool Equals(object obj) => obj is DnsMxResource record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(Preference, Exchange);

        public void ReadBytes(byte[] bytes, ref int offset, int length)
        {
            Preference = bytes.ReadUInt16(ref offset);
            Exchange = string.Join('.', bytes.ReadString(ref offset));
        }

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Preference.ToBytes();
            yield return Exchange.Split('.').ToBytes();
        }
    }
}
